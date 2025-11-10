using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Prompt & Range")]
    [Tooltip("What the prompt text should display (e.g. 'Pray', 'Collect').")]
    public string prompt = "Interact";

    [Min(0.1f)] public float requiredDistance = 2f;

    [Header("Hold Settings")]
    [Tooltip("If true, the player must hold to interact (CotL-style).")]
    public bool requiresHold = true;

    [Tooltip("Override default hold duration for this object in seconds. Set to -1 to use the global default.")]
    public float holdSecondsOverride = -1f;

    [Header("Targeting")]
    [Tooltip("Higher value wins when two targets are at similar distance.")]
    public int priority = 0;

    [Header("One-Shot Options")]
    [Tooltip("If true, this object can only be interacted with once.")]
    public bool oneShot = false;

    [Tooltip("Disable the collider after a successful one-shot interaction.")]
    public bool disableColliderOnComplete = true;

    [Header("Immediate Feedback (optional)")]
    public AudioClip interactSound;
    public GameObject visualEffectOnUse;

    [Header("Action Chain")]
    [Tooltip("Assign components implementing IInteractAction here (Destroy, Toggle, Spawn, etc.).")]
    public MonoBehaviour[] actionBehaviours;

    [Header("Events")]
    [Tooltip("Fires after all actions succeed.")]
    public UnityEvent onInteracted;

    [Header("UI Anchor (optional)")]
    [Tooltip("Optional world-space anchor for UI (e.g., hold ring). If null, we fall back to collider center, then transform.position.")]
    public Transform uiAnchor;

    bool _used;
    Collider _col;

    void Awake() => _col = GetComponent<Collider>();

    public bool CanInteract(PlayerInteractor interactor)
    {
        if (_used && oneShot) return false;
        if (!interactor) return false;
        var origin = interactor.transform.position;
        var targetCol = _col ? _col : GetComponent<Collider>();
        var closest = targetCol ? targetCol.ClosestPoint(origin) : transform.position;
        return Vector3.Distance(origin, closest) <= requiredDistance;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (_used && oneShot) return;
        int actionCount = (actionBehaviours == null) ? 0 : actionBehaviours.Length;
        Debug.Log($"[InteractableBase] Interact on {name}. Actions: {actionCount}");

        // basic feedback
        if (interactSound)
            AudioSource.PlayClipAtPoint(interactSound, transform.position);

        if (visualEffectOnUse)
            Instantiate(visualEffectOnUse, transform.position, Quaternion.identity);

        if (actionBehaviours == null || actionBehaviours.Length == 0)
        {
            Debug.Log("[InteractableBase] No actionBehaviours assigned.");
        }

        // run attached actions
        foreach (var mb in actionBehaviours)
        {
            if (!mb) { Debug.Log("[InteractableBase] Action is null, skipping."); continue; }
            Debug.Log($"[InteractableBase] Executing action: {mb.GetType().Name}");
            if (mb is IInteractAction action)
            {
                bool ok = action.Execute(interactor, this);
                Debug.Log($"[InteractableBase] {mb.GetType().Name}.Execute() returned {ok}");
                if (!ok) return; // stop chain if action blocks
            }
            else
            {
                Debug.LogWarning($"[InteractableBase] {name}: {mb.GetType().Name} doesn't implement IInteractAction.", this);
            }
        }

        // designer events
        onInteracted?.Invoke();

        // one-shot handling
        if (oneShot)
        {
            _used = true;
            if (disableColliderOnComplete && _col)
                _col.enabled = false;
        }
    }

    public string GetPrompt(PlayerInteractor interactor) => prompt;

    // Helper for selection systems that support priority bias
    public int GetPriority() => priority;

    /// <summary>
    /// Optional world position previously used for world-space UI like the hold ring.
    /// Prefers uiAnchor, then collider center, then transform.position. Safe to keep for other UI.
    /// </summary>
    public float GetEffectiveHoldSeconds(float defaultSeconds)
    {
        return (holdSecondsOverride > 0f) ? holdSecondsOverride : defaultSeconds;
    }

    public Vector3 GetUIAnchorPosition()
    {
        if (uiAnchor) return uiAnchor.position;
        var col = _col ? _col : GetComponent<Collider>();
        if (col) return col.bounds.center;
        return transform.position;
    }
}