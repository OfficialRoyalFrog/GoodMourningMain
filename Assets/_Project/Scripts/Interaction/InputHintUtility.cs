using UnityEngine;
using UnityEngine.InputSystem;

public static class InputHintUtility
{
    /// <summary>
    /// Returns a short, display-ready token for the first non-composite binding
    /// of the given action (e.g., "E", "A", "Cross", "RB").
    /// Guaranteed to omit words like "Hold", "Press", "Key", "Button".
    /// </summary>
    public static string GetShortBinding(PlayerInput playerInput, string actionName)
    {
        if (!playerInput || playerInput.actions == null) return "?";

        var action = playerInput.actions.FindAction(actionName);
        if (action == null) return "?";

        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;

            string display;

#if UNITY_INPUT_SYSTEM_1_6_OR_NEWER
            // Prefer Input System's display string but exclude device names.
            var opts = InputBinding.DisplayStringOptions.DontIncludeDevice;
            display = action.GetBindingDisplayString(i, opts);
#else
            // Older versions: derive from the effective control path.
            display = InputControlPath.ToHumanReadableString(
                b.effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice |
                InputControlPath.HumanReadableStringOptions.UseShortNames
            );
#endif
            display = Cleanup(display);
            display = Shorten(display);

            return string.IsNullOrWhiteSpace(display) ? "?" : display;
        }

        return "?";
    }

    // Remove interaction words & device suffixes the badge should never show.
    static string Cleanup(string s)
    {
        if (string.IsNullOrEmpty(s)) return "?";
        s = s.Trim();

        // strip common noise
        s = s.Replace("Hold", "").Replace("Press", "");
        s = s.Replace("Key", "").Replace("Button", "");
        s = s.Replace("Any", ""); // "Any Key" → ""
        s = s.Replace("  ", " ").Trim();

        // e.g. "E " → "E"
        return s.Trim();
    }

    // Keep badge tidy; map special keys and clamp length.
    static string Shorten(string s)
    {
        if (string.IsNullOrEmpty(s)) return "?";
        s = s.Trim();

        // Common keyboard → compact glyphs/names
        if (s.Equals("Space",   System.StringComparison.OrdinalIgnoreCase)) return "⎵";
        if (s.Equals("Enter",   System.StringComparison.OrdinalIgnoreCase)) return "↵";
        if (s.Equals("Escape",  System.StringComparison.OrdinalIgnoreCase)) return "⎋";
        if (s.Equals("Tab",     System.StringComparison.OrdinalIgnoreCase)) return "↹";
        if (s.IndexOf("Shift",  System.StringComparison.OrdinalIgnoreCase) >= 0) return "⇧";
        if (s.IndexOf("Ctrl",   System.StringComparison.OrdinalIgnoreCase) >= 0) return "Ctrl";
        if (s.IndexOf("Alt",    System.StringComparison.OrdinalIgnoreCase) >= 0) return "Alt";

        // Mouse
        if (s.IndexOf("Left",   System.StringComparison.OrdinalIgnoreCase) >= 0) return "LMB";
        if (s.IndexOf("Right",  System.StringComparison.OrdinalIgnoreCase) >= 0) return "RMB";
        if (s.IndexOf("Middle", System.StringComparison.OrdinalIgnoreCase) >= 0) return "MMB";

        // Controllers are already short (A/B/X/Y, Cross/Circle, RB, etc.)
        if (s.Length <= 3) return s;
        return s.Substring(0, 3);
    }
}