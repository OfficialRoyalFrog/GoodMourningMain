using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ResourceNode_Tree : InteractableBase, IHoldFeedback, IInteractAction, IHoldProgressProvider
{
    public enum Stage { Full, Sapling1, Sapling2, Sapling3 }

    [Header("Stage & Visuals")]
    public Stage stage = Stage.Full;
    [SerializeField] private TreeView treeView;            // assign from prefab root
    [SerializeField] private GameObject fullTreeRoot;      // optional: if you separate saplings into different prefabs
    [SerializeField] private GameObject saplingVisualRoot; // optional: if this prefab is reused for saplings

    [Header("HP (Full Tree only)")]
    [Min(1)] public int maxHp = 3;
    [SerializeField] private int currentHp = -1; // -1 = uninitialized (will set to maxHp)

    [Serializable] public struct Drop
    {
        public ItemSO item;
        public int min;
        public int max;
        [Range(0f,1f)] public float chance;
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

    [Header("Pickup")]
    public GameObject pickupPrefab;      // assign your Pickup.prefab
    public Transform dropAnchor;         // where pickups spawn (e.g., base of trunk)
    public float pickupScatterRadius = 0.2f;
    [Tooltip("Extra upward offset when spawning pickups so they are visible before magnet.")]
    public float dropSpawnHeight = 0.5f;

    [Header("Break Behavior")]
    public bool disableColliderOnBreak = true;
    public bool destroyOnBreakIfSapling = true;

    [Header("Hold Duration")]
    [Tooltip("If true, sets InteractableBase.holdSecondsOverride to remainingHP * hitIntervalSeconds on hold start so the ring fills once for the whole chop.")]
    public bool autoSetHoldDuration = true;

    [Header("Hold Tick (Full Tree)")]
    [Tooltip("Seconds between HP ticks while the player is holding.")]
    [Min(0.05f)] public float hitIntervalSeconds = 1.0f;

    bool isHolding;
    float nextHitAt;
    float carryToNextHit = 0f; // remember partial tick progress when player releases mid-hold
    [SerializeField, Range(0f,1f)] private float progress01 = 0f; // cached visual progress for UI ring

    void Awake()
    {
        // Ensure this is set up as a hold interaction unless you override in inspector.
        requiresHold = true;

        if (stage == Stage.Full)
        {
            if (currentHp < 0) currentHp = Mathf.Max(1, maxHp);
            if (treeView) treeView.ShowFull();
            if (fullTreeRoot) fullTreeRoot.SetActive(true);
            if (saplingVisualRoot) saplingVisualRoot.SetActive(false);
        }
        else
        {
            // Saplings have no chop animation and no stump—just disappear on harvest.
            if (fullTreeRoot) fullTreeRoot.SetActive(false);
            if (saplingVisualRoot) saplingVisualRoot.SetActive(true);
        }

        // Prompt default
        if (string.IsNullOrWhiteSpace(prompt)) prompt = "Chop";
    }

    void OnEnable()
    {
        if (stage == Stage.Full && currentHp < 0) currentHp = Mathf.Max(1, maxHp);
        UpdateVisualProgress(isHolding);
    }

    // === IInteractAction ===
    public bool Execute(PlayerInteractor interactor, InteractableBase owner)
    {
        // This is called when the hold completes; we are using per-second ticking while holding,
        // so for full trees we only stop the flicker here. Saplings still harvest immediately.
        if (treeView) treeView.SetChopAnimating(false);
        isHolding = false;

        if (stage != Stage.Full)
        {
            // Saplings: harvest immediately when interacted
            BreakNode(fullTree: false);
        }

        return true; // allow the action chain to continue
    }

    // === IHoldFeedback ===
    public void OnHoldStart(PlayerInteractor interactor, IInteractable target)
    {
        if ((target as Component)?.gameObject != gameObject) return;
        if (stage != Stage.Full) return; // saplings do not flicker/tick while chopping

        isHolding = true;

        if (currentHp < 0) currentHp = Mathf.Max(1, maxHp);

        // Ensure visuals match HP before resuming
        if (treeView)
        {
            if      (currentHp <= 0) treeView.ShowStump();
            else if (currentHp == maxHp) treeView.ShowFull();
            else if (currentHp == maxHp - 1) treeView.ShowDamage1();
            else                             treeView.ShowDamage2();
            treeView.SetChopAnimating(false);
        }

        // Resume tick timing
        float firstInterval = (carryToNextHit > 0f) ? Mathf.Min(carryToNextHit, hitIntervalSeconds) : hitIntervalSeconds;
        nextHitAt = Time.time + firstInterval;

        // Compute ring fill duration based on total max HP hits, keep ring scale constant
        float totalSeconds = Mathf.Max(0.05f, maxHp * hitIntervalSeconds);
        if (autoSetHoldDuration) holdSecondsOverride = totalSeconds; // keep ring scale constant across holds

        carryToNextHit = 0f; // clear any leftover partial progress once resumed

        UpdateVisualProgress(true);

        Debug.Log($"[Tree] HoldStart → HP:{currentHp}, resumeIn:{firstInterval:F2}s, ring:{holdSecondsOverride:F2}s", this);
    }

    public void OnHoldCancel(PlayerInteractor interactor, IInteractable target)
    {
        if ((target as Component)?.gameObject != gameObject) return;
        if (stage != Stage.Full) return;

        isHolding = false;
        if (treeView) treeView.SetChopAnimating(false);

        // Capture remaining time in the current tick so we can resume from it later
        carryToNextHit = Mathf.Max(0f, nextHitAt - Time.time);
        if (autoSetHoldDuration) holdSecondsOverride = -1f;

        UpdateVisualProgress(false);

        Debug.Log($"[Tree] HoldCancel → carry={carryToNextHit:F2}s", this);
    }

    void Update()
    {
        if (stage != Stage.Full) return;
        if (!isHolding) return;

        if (currentHp < 0) currentHp = Mathf.Max(1, maxHp);

        UpdateVisualProgress(true);

        if (Time.time >= nextHitAt)
        {
            currentHp = Mathf.Max(0, currentHp - 1);
            Debug.Log($"[Tree] Tick → HP now {currentHp}", this);

            carryToNextHit = 0f; // consumed a full tick, clear carry

            if (currentHp <= 0)
            {
                isHolding = false;
                if (treeView) treeView.SetChopAnimating(false);
                BreakNode(fullTree: true);
                return;
            }

            // Optional feedback frame progression
            if (treeView)
            {
                if (currentHp >= maxHp - 1) treeView.ShowDamage1();
                else                         treeView.ShowDamage2();
            }

            UpdateVisualProgress(true);

            nextHitAt += hitIntervalSeconds;
        }
    }

    // === Helpers ===
    void BreakNode(bool fullTree)
    {
        Debug.Log($"[Tree] BreakNode fullTree={fullTree}", this);

        holdSecondsOverride = -1f;

        var col = GetComponent<Collider>();

        // Visuals
        if (fullTree)
        {
            if (treeView) treeView.ShowStump();
        }
        else
        {
            // sapling: hide visuals completely
            if (saplingVisualRoot) saplingVisualRoot.SetActive(false);
            if (fullTreeRoot) fullTreeRoot.SetActive(false);
        }

        progress01 = 1f;

        // Drops
        SpawnDrops();

        // Collider
        if (disableColliderOnBreak && col) col.enabled = false;

        // One-shot behavior (using InteractableBase fields)
        if (oneShot && disableColliderOnComplete && col) col.enabled = false;

        // If you want the node to be destroyed entirely for saplings:
        if (!fullTree && destroyOnBreakIfSapling)
        {
            Destroy(gameObject, 0.01f);
        }
    }

    void SpawnDrops()
    {
        if (!pickupPrefab) return;

        var map = new Dictionary<ItemSO, int>();
        foreach (var d in dropTable)
        {
            if (!d.item) continue;
            if (UnityEngine.Random.value > d.chance) continue;

            int amt = UnityEngine.Random.Range(Mathf.Min(d.min, d.max), Mathf.Max(d.min, d.max) + 1);
            if (amt <= 0) continue;

            if (!map.ContainsKey(d.item)) map[d.item] = 0;
            map[d.item] += amt;
        }

        foreach (var kvp in map)
        {
            Vector3 pos = ComputeDropPosition();
            var go = Instantiate(pickupPrefab, pos, Quaternion.identity);
            var p = go.GetComponent<Pickup>();
            p.Init(kvp.Key, kvp.Value);
        }
    }

    Vector3 ComputeDropPosition()
    {
        // Base XY scatter
        Vector3 basePos = (dropAnchor ? dropAnchor.position : transform.position);
        Vector3 pos = basePos + UnityEngine.Random.insideUnitSphere * pickupScatterRadius;
        pos.y = basePos.y + raycastUpOffset; // start above so we can raycast down to the surface

        // If no groundLayers specified, default to Everything (~0)
        int mask = (groundLayers.value == 0) ? ~0 : groundLayers.value;

        if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, raycastDownDistance, mask, QueryTriggerInteraction.Ignore))
        {
            // Land on ground + the configured height offset so it isn't buried
            pos = hit.point;
            pos.y += Mathf.Max(0.0f, dropSpawnHeight);
        }
        else
        {
            // Fallback to old behavior (anchor height + offset)
            pos.y = basePos.y + Mathf.Max(0.0f, dropSpawnHeight);
        }

        return pos;
    }

    // === IHoldProgressProvider ===
    public float GetHoldProgress01()
    {
        return progress01;
    }

    void UpdateVisualProgress(bool isHoldingNow)
    {
        float totalSeconds = Mathf.Max(0.05f, maxHp * hitIntervalSeconds);
        int hitsDone = Mathf.Clamp(maxHp - Mathf.Max(0, currentHp), 0, maxHp);
        float elapsedThisTick = 0f;
        if (isHoldingNow)
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
        // Only report full completion when the tree is actually done (HP <= 0)
        progress01 = (currentHp <= 0) ? 1f : Mathf.Min(p, 0.999f);
    }
}