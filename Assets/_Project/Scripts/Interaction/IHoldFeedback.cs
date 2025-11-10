using UnityEngine;

public interface IHoldFeedback
{
    // Called when the hold begins on a target (locked or current).
    void OnHoldStart(PlayerInteractor interactor, IInteractable target);

    // Called when the hold is canceled before completing (finger/button released).
    void OnHoldCancel(PlayerInteractor interactor, IInteractable target);
}