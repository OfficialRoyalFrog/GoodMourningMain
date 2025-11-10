using UnityEngine;

[AddComponentMenu("Interact/Actions/Toggle Objects")]
public class ToggleObjectsAction : MonoBehaviour, IInteractAction
{
    public GameObject[] enableOnInteract;
    public GameObject[] disableOnInteract;

    public bool Execute(PlayerInteractor interactor, InteractableBase owner)
    {
        if (disableOnInteract != null)
            foreach (var go in disableOnInteract) if (go) go.SetActive(false);

        if (enableOnInteract != null)
            foreach (var go in enableOnInteract) if (go) go.SetActive(true);

        return true;
    }
}