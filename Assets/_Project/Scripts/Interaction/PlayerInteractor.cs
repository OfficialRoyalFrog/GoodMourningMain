using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Detection")]
    [Min(0.1f)] public float interactRange = 2.0f;
    [Tooltip("Optional layer filter. Leave at 0 = scan all layers.")]
    public LayerMask optionalMask;
    public bool requireLineOfSight = false;

    [Header("Selection Tuning")]
    [Tooltip("0 = no bias, 1 = very strong preference for things in front of the player/camera.")]
    [Range(0f, 1f)] public float frontPreference = 0.4f;
    [Tooltip("Leave null to use this object's forward. Assign Camera.main.transform to bias by camera facing instead.")]
    public Transform facingSource;

    [Header("Priority")]
    [Tooltip("How much a single priority point reduces the score (higher = stronger bias).")]
    public float priorityWeight = 0.25f;

    [Header("Debug")]
    public bool drawRangeGizmo = true;

    // Expose the current interactable so UI can read it.
    public IInteractable Current => _current;

    IInteractable _current;
    [Header("Debug Runtime")]
    [SerializeField] private string currentDebug = "(none)";

    void Update()
    {
        _current = FindBest();
        var comp = _current as Component;
        currentDebug = comp ? comp.name : "(none)";
    }

    // Hook this to PlayerInput → Events → Player → Interact
    public void OnInteract(InputAction.CallbackContext ctx)
    {
        // Block all world interactions when the Spirit radial is open
        if (SpiritRadialController.AnyOpen)
            return;

        // Only fire on performed to avoid double-trigger on different Press interactions
        if (!ctx.performed) return;

        var target = Current;
        if (target == null || !target.CanInteract(this)) return;

        // If the target requires a HOLD interaction, InteractHoldController will handle it.
        var comp = target as Component;
        var baseComp = comp ? comp.GetComponentInParent<InteractableBase>() : null;
        if (baseComp != null && baseComp.requiresHold)
            return;

        // Non-hold (instant) interactions go through immediately.
        target.Interact(this);
    }

    IInteractable FindBest()
    {
        int mask = (optionalMask.value == 0) ? ~0 : optionalMask.value;
        var hits = Physics.OverlapSphere(transform.position, interactRange, mask, QueryTriggerInteraction.Collide);

        float bestScore = float.MaxValue;
        IInteractable best = null;

        Vector3 p = transform.position;

        // Forward used for "in front" bias
        Vector3 fwd = facingSource ? facingSource.forward : transform.forward;
        fwd = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;

        foreach (var h in hits)
        {
            var ia = h.GetComponentInParent<IInteractable>() ?? h.GetComponent<IInteractable>();
            if (ia == null) continue;

            // Optional line-of-sight gate
            if (requireLineOfSight)
            {
                Vector3 origin = p + Vector3.up * 0.5f;
                Vector3 to = h.bounds.center - origin;
                if (Physics.Raycast(origin, to.normalized, out var hit, to.magnitude + 0.01f, ~0, QueryTriggerInteraction.Ignore))
                    if (hit.collider != h) continue;
            }

            // If the object can't be interacted with right now, skip it
            if (!ia.CanInteract(this)) continue;

            // Distance (squared) to closest point on the collider
            Vector3 closest = h.ClosestPoint(p);
            float dist2 = (closest - p).sqrMagnitude;

            // Facing bias (0..1): 1 when straight ahead, ~0 when side/behind
            Vector3 toCenter = (h.bounds.center - p);
            toCenter = Vector3.ProjectOnPlane(toCenter, Vector3.up).normalized;
            float facing01 = Mathf.Clamp01(Vector3.Dot(fwd, toCenter));

            // Reduce score for things in front by up to 'frontPreference'
            // Example: frontPreference=0.4 → straight-ahead scores ×0.6
            float bias = Mathf.Lerp(1f, 1f - frontPreference, facing01);

            float score = dist2 * bias;

            // Priority bias: higher priority lowers the score so it's chosen more often
            var asComponent = ia as Component;
            var baseComp = asComponent ? asComponent.GetComponentInParent<InteractableBase>() : null;
            if (baseComp)
            {
                score -= baseComp.priority * priorityWeight;
            }

            if (score < bestScore)
            {
                bestScore = score;
                best = ia;
            }
        }

        return best;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawRangeGizmo) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, interactRange);
    }
}