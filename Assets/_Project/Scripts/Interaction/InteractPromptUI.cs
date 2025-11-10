using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InteractPromptUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerInteractor interactor;        // Player with PlayerInteractor
    public RectTransform promptRoot;           // PromptStroke (black bar) RectTransform
    public TextMeshProUGUI label;              // PromptText (TMP)

    [Header("Behavior")]
    public bool fadeWithCanvasGroup = true;    // If true and promptGroup set, use alpha instead of SetActive

    [Header("Optional")]
    public CanvasGroup promptGroup;    // If assigned, we fade/toggle via CanvasGroup
    
    void Update()
    {
        var target = interactor ? interactor.Current : null;
        bool can = target != null && interactor.Current.CanInteract(interactor);
        bool requiresHold = false;
        if (target is Component c)
        {
            var baseComp = c.GetComponentInParent<InteractableBase>();
            requiresHold = baseComp && baseComp.requiresHold;
        }
        if (requiresHold)
            return; // HoldPromptHUD owns the UI for hold-required targets

        bool showPrompt = can; // instant actions only

        // Update label (text only, key is shown by the badge)
        if (label && showPrompt)
            label.text = target.GetPrompt(interactor); // do not clear on hide, keep last prompt text

        // Toggle visuals
        if (fadeWithCanvasGroup && promptGroup)
        {
            // Keep object active; just fade/enable raycast as needed
            promptGroup.alpha = showPrompt ? 1f : 0f;
            promptGroup.interactable = showPrompt;
            promptGroup.blocksRaycasts = showPrompt;
        }
        else
        {
            if (promptRoot) promptRoot.gameObject.SetActive(showPrompt);
        }
    }
}