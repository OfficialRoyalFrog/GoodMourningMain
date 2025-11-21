using UnityEngine;

[RequireComponent(typeof(InteractableBase))]
/// <summary>Attach to an InteractableBase action list to open the Build Menu via an interactable object.</summary>
public class BuildMenuInteractAction : MonoBehaviour, IInteractAction, IInstantInteractAction
{
    [SerializeField] private BuildMenu buildMenu;
    [SerializeField] private InventoryMenu inventoryMenu;

    InteractableBase owner;

    void Awake()
    {
        owner = GetComponent<InteractableBase>();
        ForceInstantInteract();
        ResolveRefs();
    }

    bool ResolveRefs()
    {
#if UNITY_2023_1_OR_NEWER
        if (buildMenu == null) buildMenu = FindFirstObjectByType<BuildMenu>(FindObjectsInactive.Include);
        if (inventoryMenu == null) inventoryMenu = FindFirstObjectByType<InventoryMenu>(FindObjectsInactive.Include);
#else
        if (buildMenu == null) buildMenu = FindObjectOfType<BuildMenu>();
        if (inventoryMenu == null) inventoryMenu = FindObjectOfType<InventoryMenu>();
#endif
        return buildMenu != null;
    }

    void ForceInstantInteract()
    {
        if (owner == null)
            owner = GetComponent<InteractableBase>();
        if (owner != null)
        {
            owner.requiresHold = false;
            owner.holdSecondsOverride = 0f;
            owner.forceInstant = true;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ForceInstantInteract();
    }
#endif

    public bool Execute(PlayerInteractor interactor, InteractableBase owner)
    {
        if (buildMenu == null && !ResolveRefs())
        {
            Debug.LogWarning("[BuildMenuInteractAction] No BuildMenu found in scene.");
            return false;
        }

        if (inventoryMenu != null && InventoryMenu.IsOpen)
            inventoryMenu.SetOpen(false);

        buildMenu.SetOpen(true);
        return true;
    }
}
