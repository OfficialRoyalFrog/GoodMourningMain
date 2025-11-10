using UnityEngine;

/// <summary>
/// Attaches to a Spirit hub prefab (same GameObject that has InteractableBase).
/// When the player presses Interact, this opens the SpiritRadial immediately (no hold)
/// for the spirit identified by SpiritIdHandle on the same object/children.
/// </summary>
[RequireComponent(typeof(InteractableBase))]
public class SpiritInteractOpener : MonoBehaviour, IInteractAction
{
    [Header("UI")]
    [Tooltip("Radial controller in the HUD. Drag the scene instance here.")]
    [SerializeField] private SpiritRadialController radial;

    [Header("Refs (optional auto-find)")]
    [SerializeField] private SpiritIdHandle idHandle; // auto-finds on first use if null

    private InteractableBase interactable;

    void Awake()
    {
        interactable = GetComponent<InteractableBase>();
        if (interactable)
        {
            // Ensure this interaction is instant (no hold)
            interactable.requiresHold = false;
        }

        // If not assigned in inspector, try to auto-find in the scene (works even if inactive)
        if (!radial)
        {
#if UNITY_2022_3_OR_NEWER
            radial = FindFirstObjectByType<SpiritRadialController>(FindObjectsInactive.Include);
#elif UNITY_2021_3_OR_NEWER
            radial = FindAnyObjectByType<SpiritRadialController>(FindObjectsInactive.Include);
#else
            radial = FindObjectOfType<SpiritRadialController>();
#endif
            if (!radial)
                Debug.LogWarning("[SpiritInteractOpener] Could not auto-find SpiritRadialController in scene.");
        }

        // Cache id handle if present on this or children for quicker Execute
        if (!idHandle)
            idHandle = GetComponentInChildren<SpiritIdHandle>(true);

        // Make sure this action is listed in InteractableBase.actionBehaviours so it runs.
        TryEnsureRegisteredOnInteractable();
    }

    void OnEnable()
    {
        // In case order/components changed at runtime, keep opener registered first.
        TryEnsureRegisteredOnInteractable();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Keep instant interact when editing
        var ib = GetComponent<InteractableBase>();
        if (ib) ib.requiresHold = false;
        TryEnsureRegisteredOnInteractable();
    }
#endif

    /// <summary>
    /// Adds this component into InteractableBase.actionBehaviours if it's not already present.
    /// Avoids having to remember to wire it manually on every prefab.
    /// </summary>
    private void TryEnsureRegisteredOnInteractable()
    {
        var ib = interactable ? interactable : GetComponent<InteractableBase>();
        if (!ib) return;
        var arr = ib.actionBehaviours;
        if (arr == null || arr.Length == 0)
        {
            ib.actionBehaviours = new MonoBehaviour[] { this };
            return;
        }
        // If already present, ensure it's at index 0 so the radial opens before other actions
        int existing = -1;
        for (int i = 0; i < arr.Length; i++) if (arr[i] == this) { existing = i; break; }
        if (existing == 0) return;
        if (existing > 0)
        {
            var tmp = arr[0];
            arr[0] = arr[existing];
            arr[existing] = tmp;
            ib.actionBehaviours = arr;
            return;
        }
        // Not present → insert at front
        var expanded = new MonoBehaviour[arr.Length + 1];
        expanded[0] = this;
        for (int i = 0; i < arr.Length; i++) expanded[i + 1] = arr[i];
        ib.actionBehaviours = expanded;
    }

    public bool Execute(PlayerInteractor interactor, InteractableBase owner)
    {
        if (!enabled) return true; // opener disabled, don't block chain

        if (!radial)
        {
#if UNITY_2022_3_OR_NEWER
            radial = FindFirstObjectByType<SpiritRadialController>(FindObjectsInactive.Include);
#elif UNITY_2021_3_OR_NEWER
            radial = FindAnyObjectByType<SpiritRadialController>(FindObjectsInactive.Include);
#else
            radial = FindObjectOfType<SpiritRadialController>();
#endif
            if (!radial)
            {
                Debug.LogWarning("[SpiritInteractOpener] No radial controller found in scene.");
                return true; // don't block other actions
            }
        }

        // Always refresh the handle and ID at runtime — the ID may have been assigned after Awake()
        idHandle = idHandle ? idHandle : GetComponentInChildren<SpiritIdHandle>(true);

        string id = (idHandle != null) ? idHandle.Id : null;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"[SpiritInteractOpener] SpiritIdHandle is present but ID is empty on {name}. Forcing refresh...");
            // Try to ask SpiritManager for the correct ID (if the manager already knows it)
            if (SpiritManager.Instance && SpiritManager.Instance.TryGetIdForInstance(gameObject, out var fixedId))
            {
                // Set the private backing field 'id' via reflection so future reads succeed
                var f = typeof(SpiritIdHandle).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null && idHandle != null)
                {
                    f.SetValue(idHandle, fixedId);
                }
                id = fixedId;
                Debug.Log($"[SpiritInteractOpener] ID refreshed to '{id}'.");
            }
            else
            {
                Debug.LogWarning($"[SpiritInteractOpener] Still no ID for {name}; radial will not open.");
                return true; // don't block other actions
            }
        }

        Debug.Log($"[SpiritInteractOpener] Opening radial for spirit '{id}'.");

        // Log which radial is referenced to ensure it’s using the live scene instance
        if (radial)
            Debug.Log($"[SpiritInteractOpener] radial reference = {radial.name} (scene? {radial.gameObject.scene.name})");
        else
            Debug.LogWarning("[SpiritInteractOpener] radial reference is NULL before Show(). Attempting to locate live scene instance.");

        // Safety: if radial is null or a prefab (not in a scene), try to find the scene instance again
        if (!radial || string.IsNullOrEmpty(radial.gameObject.scene.name) || radial.gameObject.scene.name == null || radial.gameObject.scene.name == "")
        {
#if UNITY_2022_3_OR_NEWER
            radial = FindFirstObjectByType<SpiritRadialController>(FindObjectsInactive.Include);
#elif UNITY_2021_3_OR_NEWER
            radial = FindAnyObjectByType<SpiritRadialController>(FindObjectsInactive.Include);
#else
            radial = FindObjectOfType<SpiritRadialController>();
#endif
            if (radial)
                Debug.Log($"[SpiritInteractOpener] Re-assigned radial to live instance: {radial.name} (scene? {radial.gameObject.scene.name})");
            else
                Debug.LogWarning("[SpiritInteractOpener] Failed to locate SpiritRadialController in the scene.");
        }

        radial.Show(id);

        // Always return true so InteractableBase continues normally
        return true;
    }
}
