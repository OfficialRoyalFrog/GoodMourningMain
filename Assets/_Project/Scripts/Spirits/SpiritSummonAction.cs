using UnityEngine;

public class SpiritSummonAction : MonoBehaviour, IInteractAction
{
    // Assigned by SpiritManager when setting up the pending instance
    public SpiritManager Manager;

    public bool Execute(PlayerInteractor interactor, InteractableBase owner)
    {
        if (Manager == null) return false;
        Manager.CompleteSummon();
        return true;
    }
}