using UnityEngine;

[AddComponentMenu("Interact/Actions/Animator Trigger")]
public class AnimatorTriggerAction : MonoBehaviour, IInteractAction
{
    public Animator animator;
    public string triggerName = "Open";

    public bool Execute(PlayerInteractor interactor, InteractableBase owner)
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator && !string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);
        return true;
    }
}