using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple mineable rock that behaves like the tree resource node but without stage art.
/// Requires the player to hold the interact button; each tick removes HP until the rock
/// breaks and spawns pickups from its drop table.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ResourceNode_Rock : InteractableBase, IHoldFeedback, IInteractAction, IHoldProgressProvider
{
    [Header("Visuals")]
    [Tooltip("Optional root (mesh, sprite, etc.) that gets disabled when the rock is depleted.")]
    [SerializeField] private GameObject visualsRoot;

    [Header("HP")]
    [Min(1)] public int maxHp = 4;
    [SerializeField] private int currentHp = -1; // -1 = lazy init to maxHp

    [Serializable]
    public struct Drop
    {
        public ItemSO item;
        public int min;
        public int max;
        [Range(0f, 1f)] public float chance;
    }

    [Header("Drops")]
    public List<Drop> dropTable = new();

    [Header("Drop Spawning")]
    [Tooltip("Layers considered ground when raycasting to place drops")]
    public LayerMask groundLayers;
    [Tooltip("How far above to start the downward ray when placing drops")]
    public float raycastUpOffset = 2f;
    [Tooltip("How far down to search for ground when placing drops")]
    public float raycastDownDistance = 20f;
    public GameObject pickupPrefab;
    public Transform dropAnchor;
    public float pickupScatterRadius = 0.25f;
    [Tooltip("Extra upward offset when spawning pickups so they are visible before magnet.")]
    public float dropSpawnHeight = 0.4f;

    [Header("Break Behavior")]
    public bool disableColliderOnBreak = true;
    public bool destroyOnBreak = false;

    [Header("Hold Timing")]
    [Tooltip("Seconds between HP ticks while holding the interact button.")]
    [Min(0.05f)] public float hitIntervalSeconds = 0.75f;
    [Tooltip("Automatically scale the hold ring so it matches the total time to break this node.")]
    public bool autoSetHoldDuration = true;

    bool isHolding;
    float nextHitAt;
    float carryToNextHit = 0f;
    [SerializeField, Range(0f, 1f)] private float progress01 = 0f;

    void Awake()
    {
        requiresHold = true;

        if (currentHp < 0) currentHp = Mathf.Max(1, maxHp);
        if (string.IsNullOrWhiteSpace(prompt)) prompt = "Mine";
    }

    void OnEnable()
    {
        if (currentHp < 0) currentHp = Mathf.Max(1, maxHp);
        UpdateVisualProgress(false);

        if (visualsRoot)
            visualsRoot.SetActive(true);

        var col = GetComponent<Collider>();
        if (col && disableColliderOnBreak)
            col.enabled = true;
    }

    // === IInteractAction ===
    public bool Execute(PlayerInteractor interactor, InteractableBase owner)
    {
        isHolding = false;
        if (holdSecondsOverride > 0f) holdSecondsOverride = -1f;
        return true;
    }

    // === IHoldFeedback ===
    public void OnHoldStart(PlayerInteractor interactor, IInteractable target)
    {
        if (!IsSelf(target) || currentHp <= 0) return;

        isHolding = true;
        if (currentHp < 0) currentHp = Mathf.Max(1, maxHp);

        float firstInterval = (carryToNextHit > 0f)
            ? Mathf.Min(carryToNextHit, hitIntervalSeconds)
            : hitIntervalSeconds;
        nextHitAt = Time.time + firstInterval;

        if (autoSetHoldDuration)
        {
            float totalSeconds = Mathf.Max(0.05f, maxHp * hitIntervalSeconds);
            holdSecondsOverride = totalSeconds;
        }

        carryToNextHit = 0f;
        UpdateVisualProgress(true);
    }

    public void OnHoldCancel(PlayerInteractor interactor, IInteractable target)
    {
        if (!IsSelf(target)) return;

        isHolding = false;
        carryToNextHit = Mathf.Max(0f, nextHitAt - Time.time);
        if (autoSetHoldDuration) holdSecondsOverride = -1f;

        UpdateVisualProgress(false);
    }

    void Update()
    {
        if (!isHolding || currentHp <= 0) return;

        UpdateVisualProgress(true);

        if (Time.time >= nextHitAt)
        {
            currentHp = Mathf.Max(0, currentHp - 1);
            carryToNextHit = 0f;

            if (currentHp <= 0)
            {
                isHolding = false;
                BreakNode();
                return;
            }

            nextHitAt += hitIntervalSeconds;
        }
    }

    void BreakNode()
    {
        holdSecondsOverride = -1f;
        progress01 = 1f;

        if (visualsRoot) visualsRoot.SetActive(false);

        SpawnDrops();

        var col = GetComponent<Collider>();
        if (disableColliderOnBreak && col) col.enabled = false;

        if (destroyOnBreak)
            Destroy(gameObject, 0.05f);
    }

    void SpawnDrops()
    {
        if (!pickupPrefab) return;

        var totals = new Dictionary<ItemSO, int>();
        foreach (var drop in dropTable)
        {
            if (!drop.item) continue;
            if (UnityEngine.Random.value > drop.chance) continue;

            int amount = UnityEngine.Random.Range(
                Mathf.Min(drop.min, drop.max),
                Mathf.Max(drop.min, drop.max) + 1);
            if (amount <= 0) continue;

            if (!totals.ContainsKey(drop.item)) totals[drop.item] = 0;
            totals[drop.item] += amount;
        }

        foreach (var kvp in totals)
        {
            Vector3 pos = ComputeDropPosition();
            var pickup = Instantiate(pickupPrefab, pos, Quaternion.identity);
            var pickupComp = pickup.GetComponent<Pickup>();
            if (pickupComp != null)
                pickupComp.Init(kvp.Key, kvp.Value);
        }
    }

    Vector3 ComputeDropPosition()
    {
        Vector3 basePos = dropAnchor ? dropAnchor.position : transform.position;
        Vector3 pos = basePos + UnityEngine.Random.insideUnitSphere * pickupScatterRadius;
        pos.y = basePos.y + raycastUpOffset;

        int mask = (groundLayers.value == 0) ? ~0 : groundLayers.value;
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, raycastDownDistance, mask, QueryTriggerInteraction.Ignore))
        {
            pos = hit.point;
            pos.y += Mathf.Max(0f, dropSpawnHeight);
        }
        else
        {
            pos.y = basePos.y + Mathf.Max(0f, dropSpawnHeight);
        }

        return pos;
    }

    bool IsSelf(IInteractable target)
    {
        return (target as Component)?.gameObject == gameObject;
    }

    // === IHoldProgressProvider ===
    public float GetHoldProgress01() => progress01;

    void UpdateVisualProgress(bool holdingNow)
    {
        float totalSeconds = Mathf.Max(0.05f, maxHp * hitIntervalSeconds);
        int hitsDone = Mathf.Clamp(maxHp - Mathf.Max(0, currentHp), 0, maxHp);
        float elapsedThisTick;
        if (holdingNow)
        {
            float remaining = Mathf.Max(0f, nextHitAt - Time.time);
            elapsedThisTick = Mathf.Clamp(hitIntervalSeconds - remaining, 0f, hitIntervalSeconds);
        }
        else
        {
            elapsedThisTick = Mathf.Clamp(hitIntervalSeconds - carryToNextHit, 0f, hitIntervalSeconds);
        }

        float completedSeconds = hitsDone * hitIntervalSeconds + elapsedThisTick;
        float p = Mathf.Clamp01(completedSeconds / totalSeconds);
        progress01 = (currentHp <= 0) ? 1f : Mathf.Min(p, 0.999f);
    }
}
