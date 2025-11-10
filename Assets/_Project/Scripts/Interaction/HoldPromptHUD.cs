using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KD.UI
{
    /// <summary>
    /// Bottom-HUD prompt that shows: "Hold (E) to Chop Tree" with a radial fill around the input glyph.
    /// Pure view: no input or gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoldPromptHUD : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private CanvasGroup group;          // CanvasGroup on the root
        [SerializeField] private Image glyphImage;           // Button glyph icon (optional)
        [SerializeField] private Image radialImage;          // Image.type=Filled, FillMethod=Radial360
        [SerializeField] private TextMeshProUGUI label;      // Legacy TMP label "Hold (E) to Chop"
        [SerializeField] private TextMeshProUGUI prefixLabel;// Optional "Hold" label for split layout
        [SerializeField] private TextMeshProUGUI suffixLabel;// Optional "to Chop Tree" label for split layout
        [SerializeField] private TextMeshProUGUI bindingLabel;// Optional binding text rendered inside the radial

        [Header("Split Label Copy")]
        [SerializeField] private string holdPrefixText = "Hold";
        [SerializeField] private string promptFormat = "to {0}";

        [Header("Behavior")]
        [SerializeField, Min(0f)] private float fadeSeconds = 0.12f; // UI fade
        [SerializeField, Range(0f, 1f)] private float minAlphaToBlockRaycasts = 0.05f;

        // internal
        float targetAlpha;
        float fadeVel;
        readonly StringBuilder sb = new StringBuilder(64);

        // cached label parts to avoid unnecessary string rebuilds
        string cachedBindingText = "?";
        string cachedPromptText  = string.Empty;

        public bool IsVisible => group && group.alpha > 0.001f;

        void Reset()
        {
            group = GetComponent<CanvasGroup>();
        }

        void Awake()
        {
            if (!group) group = GetComponent<CanvasGroup>();

            // Defensive defaults
            if (radialImage)
            {
                radialImage.type = Image.Type.Filled;
                radialImage.fillMethod = Image.FillMethod.Radial360;
                radialImage.fillOrigin = (int)Image.Origin360.Top;
                radialImage.fillClockwise = true;
                radialImage.fillAmount = 0f;
            }

            if (group)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            ApplyLabel(cachedBindingText, cachedPromptText, force:true);
        }

        void Update()
        {
            if (!group) return;

            // unscaled time so the fade isn't affected by pause/time scale
            if (!Mathf.Approximately(group.alpha, targetAlpha))
            {
                group.alpha = Mathf.SmoothDamp(
                    group.alpha,
                    targetAlpha,
                    ref fadeVel,
                    Mathf.Max(0.0001f, fadeSeconds),
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);

                bool block = group.alpha >= minAlphaToBlockRaycasts;
                group.blocksRaycasts = block;
                group.interactable   = block;
            }
        }

        /// <summary>
        /// Show the HUD and set the copy. Example: binding="E", prompt="Chop Tree" → "Hold (E) to Chop Tree".
        /// Ensures the relevant objects and parents up to this HUD are active.
        /// </summary>
        public void Show(string bindingText, string prompt)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            EnsureActiveUpToSelf(label ? label.gameObject : null);
            EnsureActiveUpToSelf(prefixLabel ? prefixLabel.gameObject : null);
            EnsureActiveUpToSelf(suffixLabel ? suffixLabel.gameObject : null);
            EnsureActiveUpToSelf(bindingLabel ? bindingLabel.gameObject : null);
            EnsureActiveUpToSelf(radialImage ? radialImage.gameObject : null);

            ApplyLabel(bindingText, prompt, force:true); // always rewrite label so revisits don't show an empty prompt
            SetProgress01(0f);
            FadeTo(1f);
        }

        /// <summary>
        /// Hide the HUD and reset progress to 0.
        /// </summary>
        public void Hide()
        {
            SetProgress01(0f);
            FadeTo(0f);
        }

        /// <summary>
        /// 0..1 radial progress around the glyph.
        /// </summary>
        public void SetProgress01(float value01)
        {
            if (!radialImage) return;
            radialImage.fillAmount = Mathf.Clamp01(value01);
        }

        /// <summary>
        /// Optional. Provide a sprite glyph for the current device binding.
        /// If null, the glyphImage is disabled and only the text "(E)" shows.
        /// </summary>
        public void SetBindingGlyph(Sprite glyphOrNull)
        {
            if (!glyphImage) return;
            glyphImage.sprite = glyphOrNull;
            glyphImage.enabled = glyphOrNull != null;
        }

        /// <summary>
        /// Update just the binding token "(E)". Keeps current prompt text.
        /// </summary>
        public void SetBindingText(string bindingText)
        {
            ApplyLabel(bindingText, cachedPromptText, force:false);
        }

        /// <summary>
        /// Update just the prompt text "Chop Tree". Keeps current binding token.
        /// </summary>
        public void SetPromptText(string prompt)
        {
            ApplyLabel(cachedBindingText, prompt, force:false);
        }

        /// <summary>
        /// Immediately set the current alpha. Avoids a frame of pop on first show.
        /// Also ensures the GameObject and key children are active when showing.
        /// </summary>
        public void SnapVisible(bool visible)
        {
            if (!group) return;
            if (visible && !gameObject.activeSelf) gameObject.SetActive(true);
            if (visible)
            {
                EnsureActiveUpToSelf(label ? label.gameObject : null);
                EnsureActiveUpToSelf(prefixLabel ? prefixLabel.gameObject : null);
                EnsureActiveUpToSelf(suffixLabel ? suffixLabel.gameObject : null);
                EnsureActiveUpToSelf(bindingLabel ? bindingLabel.gameObject : null);
                EnsureActiveUpToSelf(radialImage ? radialImage.gameObject : null);
            }
            targetAlpha = visible ? 1f : 0f;
            group.alpha = targetAlpha;
            bool block = group.alpha >= minAlphaToBlockRaycasts;
            group.blocksRaycasts = block;
            group.interactable   = block;
        }

        // ----- internals -----

        void FadeTo(float a)
        {
            targetAlpha = Mathf.Clamp01(a);
            // Update interactable flags immediately to avoid stray clicks
            bool block = targetAlpha >= minAlphaToBlockRaycasts;
            if (group)
            {
                group.blocksRaycasts = block;
                group.interactable   = block;
            }
        }

        void ApplyLabel(string bindingText, string prompt, bool force)
        {
            // Normalize inputs
            bindingText = string.IsNullOrWhiteSpace(bindingText) ? "?" : bindingText.Trim();
            prompt      = prompt?.Trim() ?? string.Empty;

            if (!force && bindingText == cachedBindingText && prompt == cachedPromptText)
                return;

            cachedBindingText = bindingText;
            cachedPromptText  = prompt;

            if (label)
            {
                sb.Clear();
                sb.Append("Hold (");
                sb.Append(bindingText);
                sb.Append(") to ");
                sb.Append(prompt);
                label.text = sb.ToString();
            }

            if (prefixLabel)
                prefixLabel.text = string.IsNullOrWhiteSpace(holdPrefixText) ? "Hold" : holdPrefixText;

            if (suffixLabel)
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    suffixLabel.text = string.Empty;
                else
                    suffixLabel.text = FormatSuffix(prompt);
            }

            if (bindingLabel)
                bindingLabel.text = bindingText;
        }

        string FormatSuffix(string prompt)
        {
            if (string.IsNullOrWhiteSpace(promptFormat))
                return prompt;
            try
            {
                return string.Format(promptFormat, prompt);
            }
            catch
            {
                return prompt;
            }
        }

        // Ensure a target and its parents up to this HUD are active
        void EnsureActiveUpToSelf(GameObject go)
        {
            if (!go) return;
            Transform stop = this.transform;
            Transform t = go.transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                    t.gameObject.SetActive(true);
                if (t == stop) break;
                t = t.parent;
            }
        }
    }
}
