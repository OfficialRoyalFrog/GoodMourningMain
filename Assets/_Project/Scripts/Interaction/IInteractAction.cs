using UnityEngine;

public interface IInteractAction
{
    // Return true if the action completed successfully; false can abort the chain if desired.
    // If you need time (animations), do the work via Coroutine on your MonoBehaviour then return true immediately.
    bool Execute(PlayerInteractor interactor, InteractableBase owner);
}