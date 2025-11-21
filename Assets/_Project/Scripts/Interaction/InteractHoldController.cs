using System.Collections;
using KD.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace KD.Core
{
    /// <summary>
    /// Coordinates player input hold interactions.
    /// Handles timing, freeze/unfreeze, and HUD updates.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractHoldController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInteractor interactor;   // player target finder
        [SerializeField] private PlayerMover mover;             // movement script to freeze
        [SerializeField] private HoldPromptHUD hud;             // bottom HUD prompt

        [Header("Defaults")]
        [SerializeField, Min(0.05f)] private float defaultHoldSeconds = 0.65f;

        [Header("Events")]
        public UnityEvent OnHoldStarted;
        public UnityEvent OnHoldCanceled;
        public UnityEvent OnHoldCompleted;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;
        [SerializeField] private bool snapVisibleOnFocus = true;
        [SerializeField] private bool delayHoldStartByFrame = true; // ensures focus is set before BeginHold runs

        IInteractable currentTarget;
        bool isHolding;
        float timer;
        float holdDuration;

        bool hasFocus => interactor && interactor.Current != null;
        bool canInteract => hasFocus && interactor.Current.CanInteract(interactor);

        void Awake()
        {
            if (!hud)
#if UNITY_2023_1_OR_NEWER
                hud = Object.FindFirstObjectByType<HoldPromptHUD>();
#else
                hud = Object.FindObjectOfType<HoldPromptHUD>();
#endif
        }

        void Update()
        {
            RefreshFocus();
            if (!isHolding) return;

            if (currentTarget == null || !currentTarget.CanInteract(interactor))
            {
                CancelHold();
                return;
            }

            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / Mathf.Max(holdDuration, 0.001f));
            hud?.SetProgress01(progress);

            if (progress >= 1f)
                CompleteHold();
        }

        public void BeginHold()
        {
            if (delayHoldStartByFrame)
            {
                if (debugLogs) Debug.Log("[Hold] Queued BeginHold for next frame", this);
                StartCoroutine(BeginHoldNextFrame());
                return;
            }

            StartHoldInternal();
        }

        private IEnumerator BeginHoldNextFrame()
        {
            yield return null; // wait one frame for focus to update
            StartHoldInternal();
        }

        private void StartHoldInternal()
        {
            if (isHolding || !canInteract)
            {
                if (debugLogs) Debug.Log($"[Hold] BeginHold gated. isHolding={isHolding}, canInteract={canInteract}", this);
                return;
            }

            currentTarget = interactor.Current;
            if (debugLogs) Debug.Log($"[Hold] BeginHold target={(currentTarget as Component ? ((Component)currentTarget).name : "?")}", this);

            timer = 0f;
            isHolding = true;

            if (mover) mover.enabled = false;

            TrySendHoldFeedback(currentTarget, start: true);

            holdDuration = ResolveHoldSeconds(currentTarget);

            string prompt = currentTarget.GetPrompt(interactor);
            string binding = InputHintUtility.GetShortBinding(FindPlayerInput(), "Interact");

            hud?.Show(binding, prompt);
            if (snapVisibleOnFocus) hud?.SnapVisible(true);
            float resumeProgress = InitializeHoldProgress(currentTarget);
            hud?.SetProgress01(resumeProgress);

            OnHoldStarted?.Invoke();
        }

        public void CancelHold()
        {
            if (debugLogs) Debug.Log("[Hold] CancelHold", this);
            if (!isHolding) return;
            isHolding = false;
            timer = 0f;

            if (mover) mover.enabled = true;
            hud?.Hide();
            TrySendHoldFeedback(currentTarget, start: false);
            currentTarget = null;
            OnHoldCanceled?.Invoke();
        }

        void CompleteHold()
        {
            isHolding = false;
            timer = 0f;

            if (mover) mover.enabled = true;
            hud?.Hide();

            var target = currentTarget;
            currentTarget = null;
            target?.Interact(interactor);
            OnHoldCompleted?.Invoke();
        }

        void RefreshFocus()
        {
            var focus = interactor ? interactor.Current : null;

            if (focus == null || (focus is Object unityObj && unityObj == null))
            {
                if (!isHolding)
                    hud?.Hide();
                return;
            }

            string prompt = focus.GetPrompt(interactor);
            string binding = InputHintUtility.GetShortBinding(FindPlayerInput(), "Interact");

            if (!isHolding)
            {
                bool requiresHold = false;
                if (focus is Component c)
                {
                    if (!c)
                    {
                        if (!isHolding)
                            hud?.Hide();
                        return;
                    }
                    var baseComp = c.GetComponentInParent<InteractableBase>();
                    requiresHold = baseComp && baseComp.RequiresHoldRuntime;
                }

                if (requiresHold)
                {
                    if (debugLogs)
                        Debug.Log($"[HoldHUD] SHOW prompt=\"{prompt}\" focus={(focus as Component ? ((Component)focus).name : "?")}", this);
                    hud?.Show(binding, prompt, true);
                    hud?.SetProgress01(0f);
                    if (snapVisibleOnFocus) hud?.SnapVisible(true);
                }
                else
                {
                    if (debugLogs) Debug.Log("[HoldHUD] SHOW instant (not a hold target)", this);
                    hud?.Show(binding, prompt, false);
                    hud?.SetProgress01(0f);
                    if (snapVisibleOnFocus) hud?.SnapVisible(true);
                }
            }
        }

        PlayerInput FindPlayerInput()
        {
            if (!interactor) return null;
            return interactor.GetComponent<PlayerInput>();
        }

        void TrySendHoldFeedback(IInteractable target, bool start)
        {
            if (target is not Component c || !c) return;
            var feedback = c.GetComponent<IHoldFeedback>();
            if (feedback == null) return;

            if (start)
                feedback.OnHoldStart(interactor, target);
            else
                feedback.OnHoldCancel(interactor, target);
        }

        float ResolveHoldSeconds(IInteractable target)
        {
            float duration = Mathf.Max(0.05f, defaultHoldSeconds);
            if (target is Component c && c)
            {
                var baseComp = c.GetComponentInParent<InteractableBase>();
                if (baseComp && baseComp.holdSecondsOverride > 0f)
                    duration = baseComp.holdSecondsOverride;
            }
            return duration;
        }

        float InitializeHoldProgress(IInteractable target)
        {
            if (holdDuration <= 0.0001f)
            {
                timer = 0f;
                return 0f;
            }

            float progress = 0f;
            if (target is Component c && c)
            {
                var provider = c.GetComponent<IHoldProgressProvider>();
                if (provider != null)
                {
                    progress = Mathf.Clamp01(provider.GetHoldProgress01());
                    timer = holdDuration * progress;
                }
            }

            return progress;
        }
    }
}
