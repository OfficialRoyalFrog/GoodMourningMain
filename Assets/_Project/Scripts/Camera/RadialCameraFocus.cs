using UnityEngine;
using Unity.Cinemachine; // new namespace in CM3

/// <summary>
/// Temporarily reframes the camera to include two subjects (player + spirit)
/// and applies a gentle zoom while the radial menu is open. Restores on close.
/// Compatible with Cinemachine 3.x (Unity 6+).
/// </summary>
public class RadialCameraFocus : MonoBehaviour
{
    [Header("Cinemachine 3.x Reframe")]
    [SerializeField] private CinemachineCamera vcam;              // assign your main vcam (e.g., CM_Main)

    [Header("Zoom Amounts")]
    [Tooltip("Perspective cameras: delta FOV (negative = zoom in)")] 
    [SerializeField] private float fovDelta = -8f;
    [Tooltip("Orthographic cameras: delta size (negative = zoom in)")] 
    [SerializeField] private float orthoSizeDelta = -1.25f;

    [Header("Timing (Unscaled)")]
    [Tooltip("Seconds to zoom in when the radial opens")] 
    [SerializeField] private float zoomInSeconds = 0.15f;
    [Tooltip("Seconds to zoom out when the radial closes")] 
    [SerializeField] private float zoomOutSeconds = 0.20f;

    [Header("Debug")] 
    [SerializeField] private bool enableLogs = false;

    // Stored state
    private float prevFov;
    private float prevOrthoSize;
    private int prevPriority;
    private bool prevOrtho;
    private bool active;

    void Awake()
    {
        if (!vcam)
        {
            var vcamsAll = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            foreach (var cam in vcamsAll)
            {
                if (cam.name == "CM_Main") { vcam = cam; break; }
            }
            if (!vcam && vcamsAll.Length > 0)
                vcam = vcamsAll[0];
        }
    }

    public void Apply(Transform player, Transform spirit)
    {
        if (enableLogs) Debug.Log($"[RadialCameraFocus] Apply() called. player={(player ? player.name : "null")}, spirit={(spirit ? spirit.name : "null")}");
        if (!vcam || player == null || spirit == null) return;

        // Save previous camera state
        prevPriority = vcam.Priority;
        prevOrtho = vcam.Lens.Orthographic;
        prevFov = vcam.Lens.FieldOfView;
        prevOrthoSize = vcam.Lens.OrthographicSize;

        // Raise priority to take control
        int maxPriority = prevPriority;
        var vcamsRuntime = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (var cam in vcamsRuntime)
            if (cam.Priority > maxPriority) maxPriority = cam.Priority;
        vcam.Priority = maxPriority + 10;

        // Zoom
        StopAllCoroutines();
        if (prevOrtho)
            StartCoroutine(LerpOrtho(prevOrthoSize, Mathf.Max(0.5f, prevOrthoSize + orthoSizeDelta), zoomInSeconds, null));
        else
            StartCoroutine(LerpFov(prevFov, Mathf.Clamp(prevFov + fovDelta, 20f, 80f), zoomInSeconds, null));

        if (enableLogs) Debug.Log($"[RadialCameraFocus] Apply on {vcam.name} (mode={(prevOrtho?"Ortho":"Persp")})");
        active = true;
    }

    public void Clear()
    {
        if (!vcam || !active) return;
        if (enableLogs) Debug.Log("[RadialCameraFocus] Clear() called");
        StopAllCoroutines();
        if (prevOrtho)
            StartCoroutine(LerpOrtho(vcam.Lens.OrthographicSize, prevOrthoSize, zoomOutSeconds, OnRestoreComplete));
        else
            StartCoroutine(LerpFov(vcam.Lens.FieldOfView, prevFov, zoomOutSeconds, OnRestoreComplete));
    }

    private void OnRestoreComplete()
    {
        vcam.Priority = prevPriority;
        active = false;
        if (enableLogs) Debug.Log("[RadialCameraFocus] Restore complete");
    }

    private System.Collections.IEnumerator LerpFov(float from, float to, float seconds, System.Action onComplete)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = (seconds <= 0.0001f) ? 1f : Mathf.Clamp01(t / seconds);
            vcam.Lens.FieldOfView = Mathf.Lerp(from, to, k);
            yield return null;
        }
        vcam.Lens.FieldOfView = to;
        onComplete?.Invoke();
    }

    private System.Collections.IEnumerator LerpOrtho(float from, float to, float seconds, System.Action onComplete)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = (seconds <= 0.0001f) ? 1f : Mathf.Clamp01(t / seconds);
            vcam.Lens.OrthographicSize = Mathf.Lerp(from, to, k);
            yield return null;
        }
        vcam.Lens.OrthographicSize = to;
        onComplete?.Invoke();
    }
}