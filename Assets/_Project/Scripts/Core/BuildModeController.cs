using UnityEngine;

public class BuildModeController : MonoBehaviour
{
    public static bool IsBuildMode { get; private set; }

    public void ToggleBuildMode()
    {
        if (GamePause.IsPaused) return;
        IsBuildMode = !IsBuildMode;
        // TODO: later: enable/disable Build HUD + ghost
    }
}