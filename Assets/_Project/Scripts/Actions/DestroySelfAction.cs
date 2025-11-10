using UnityEngine;

[AddComponentMenu("Interact/Actions/Destroy Self")]
public class DestroySelfAction : MonoBehaviour, IInteractAction
{
    public float delay = 0.0f;
    public bool Execute(PlayerInteractor interactor, InteractableBase owner)
    {
        if (delay <= 0f) Destroy(owner.gameObject);
        else Destroy(owner.gameObject, delay);
        return true;
    }
}