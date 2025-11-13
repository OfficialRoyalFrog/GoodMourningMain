using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using Game.Core.TimeSystem;

/// <summary>
/// Lightweight runtime HUD that surfaces perf + gameplay stats directly in-camera.
/// Drop this on a small Canvas/Panel with a TMP_Text child and wire the text field.
/// Press the toggle key in play mode to hide/show without touching the inspector.
/// </summary>
[ExecuteAlways]
public class DebugOverlayUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text readout;
    [SerializeField] private CanvasGroup overlayGroup;

    [Header("Behavior")]
    [Tooltip("Updates per second. Lower = cheaper but more latent numbers.")]
    [SerializeField, Range(1f, 10f)] private float refreshRate = 4f;
    [Tooltip("Optional key to toggle visibility in play mode.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [Tooltip("Hide automatically when entering Play Mode (press the toggle key to reveal).")]
    [SerializeField] private bool startHiddenInPlayMode = false;

    [Header("Spirits")]
    [Tooltip("If true, counts owned spirits via SpiritManager (\"ghosts\" readout).")]
    [SerializeField] private bool showSpiritCount = true;

    private readonly StringBuilder builder = new StringBuilder(256);
    private float refreshTimer;
    private bool isVisible = true;

    private void Awake()
    {
        if (!readout)
            readout = GetComponentInChildren<TMP_Text>(true);

        if (!overlayGroup)
            overlayGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (Application.isPlaying && startHiddenInPlayMode)
        {
            isVisible = false;
            ApplyVisibility();
        }
        else
        {
            isVisible = true;
            ApplyVisibility();
        }

        // Ensure first frame shows data in edit mode too.
        ForceRefresh();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            // keep numbers fresh in edit mode (e.g., layout preview)
            ForceRefresh();
            return;
        }

        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            ApplyVisibility();
        }

        if (!isVisible)
            return;

        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= (1f / refreshRate))
        {
            refreshTimer = 0f;
            ForceRefresh();
        }
    }

    private void ForceRefresh()
    {
        if (readout == null) return;

        builder.Clear();

        // FPS + frame time (unscaled so it ignores pauses)
        float fps = (Time.unscaledDeltaTime > 0f) ? (1f / Time.unscaledDeltaTime) : 0f;
        float frameMs = Time.unscaledDeltaTime * 1000f;
        builder.AppendFormat("FPS {0:0.#}  ({1:0.0} ms)\n", fps, frameMs);
        builder.AppendFormat("TimeScale {0:0.##}\n", Time.timeScale);

        // World clock
        var timeMgr = TimeManager.Instance;
        if (timeMgr != null)
            builder.AppendFormat("Day {0}  {1:00}:{2:00}\n", timeMgr.DayIndex, timeMgr.Hour, timeMgr.Minute);

        // Spirits / ghosts
        if (showSpiritCount && SpiritManager.Instance != null)
        {
            int owned = SpiritManager.Instance.OwnedSpiritIds?.Count ?? 0;
            builder.AppendFormat("Spirits {0}\n", owned);
        }

        // Memory (MB)
        long allocated = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        long reserved = Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
        builder.AppendFormat("Memory {0} / {1} MB\n", allocated, reserved);

        readout.text = builder.ToString();
    }

    private void ApplyVisibility()
    {
        if (overlayGroup)
        {
            overlayGroup.alpha = isVisible ? 1f : 0f;
            overlayGroup.interactable = isVisible;
            overlayGroup.blocksRaycasts = isVisible;
        }
        else if (readout)
        {
            readout.enabled = isVisible;
        }
    }
}
