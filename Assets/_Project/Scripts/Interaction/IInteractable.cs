using UnityEngine;

public interface IInteractable
{
    bool CanInteract(PlayerInteractor interactor);
    void Interact(PlayerInteractor interactor);
    string GetPrompt(PlayerInteractor interactor);
}