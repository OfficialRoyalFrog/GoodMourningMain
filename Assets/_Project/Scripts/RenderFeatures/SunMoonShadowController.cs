using UnityEngine;
using UnityEngine.Rendering;
using Game.Core.TimeSystem;

// PURPOSE: Rotate ONLY the shadow-casting directional light so shadows move across the day.
// This component DOES NOT touch: intensities, colors, volumes, shadow strength, or RenderSettings.sun.
[DisallowMultipleComponent]
[DefaultExecutionOrder(110)] // after TimeManager
public sealed class SunMoonShadowController : MonoBehaviour
{
    [Header("Shadow Light (only direction is changed)")]
    [SerializeField] private Light shadowLight; // Directional light that already casts your shadows

    [Header("Rotation")]
    [Tooltip("Azimuth at t=0 (start of day). 0 = +Z, 90 = +X. Adjust to match your scene.")]
    [SerializeField] private float azimuthAtStart = 135f;
    [Tooltip("If true, azimuth decreases as time advances (clockwise).")]
    [SerializeField] private bool azimuthClockwise = true;

    [Tooltip("Minimum elevation (deg) at dawn/dusk.")]
    [SerializeField, Range(0f, 89f)] private float elevationMin = 25f;
    [Tooltip("Maximum elevation (deg) at midday.")]
    [SerializeField, Range(0f, 89f)] private float elevationMax = 55f;

    [Header("Smoothing (rotation only)")]
    [SerializeField] private bool  enableSmoothing = true;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.25f; // seconds to ~63% toward target

    [Header("Utilities")]
    [SerializeField] private bool autoAssignFromRenderSettingsSun = true;
    [SerializeField] private bool warnIfNotCastingShadows = true;

    [Header("Shadow Strength (optional)")]
    [SerializeField] private bool controlShadowStrength = true;
    [SerializeField, Range(0f,1f)] private float dayShadowStrength  = 0.05f; // nearly invisible
    [SerializeField, Range(0f,1f)] private float nightShadowStrength = -1f;  // -1 = use current value at OnEnable

    [Tooltip("If assigned, we read nightVolume.weight (from DayNightBlend) to match your exact night window.")]
    [SerializeField] private Volume nightVolume; // read-only; we never modify

    [Tooltip("If no Night Volume is assigned, compute night factor from TimeManager.Sunset->Sunrise with this feather (minutes).")]
    [SerializeField, Range(0,180)] private int fallbackFeatherMinutes = 60;

    private bool _seeded;

    private void Reset()
    {
        if (!shadowLight) shadowLight = RenderSettings.sun;
    }

    private void OnEnable()
    {
        _seeded = false; // rotation will snap once, then smooth subsequently
        if (shadowLight == null && autoAssignFromRenderSettingsSun)
        {
            shadowLight = RenderSettings.sun;
        }
        if (shadowLight != null && nightShadowStrength < 0f)
        {
            nightShadowStrength = Mathf.Clamp01(shadowLight.shadowStrength);
        }
    }

    private void Update()
    {
        if (TimeManager.Instance == null || shadowLight == null) return;
        if (shadowLight.type != LightType.Directional) return; // we only rotate directionals

        if (warnIfNotCastingShadows)
        {
            // Warn if configuration prevents visible moving shadows
            if (shadowLight.shadows == LightShadows.None)
                Debug.LogWarning("SunMoonShadowController: '" + shadowLight.name + "' has Shadows=None. Enable Soft/Hard to see moving shadows.");

            var bake = shadowLight.bakingOutput.lightmapBakeType;
            if (bake == UnityEngine.LightmapBakeType.Baked)
                Debug.LogWarning("SunMoonShadowController: '" + shadowLight.name + "' is Baked. Use Realtime or Mixed for moving shadows.");

            if (QualitySettings.shadowDistance < 20f)
                Debug.LogWarning("SunMoonShadowController: QualitySettings.shadowDistance is very low (" + QualitySettings.shadowDistance + "). Increase to see distant shadows.");
        }

        // 0..1 across the current day (same source your clock uses)
        float t = Mathf.Repeat(TimeManager.Instance.NormalizedDay, 1f);

        // Azimuth spins 360° per day
        float dir = azimuthClockwise ? -1f : 1f;
        float azimuth = azimuthAtStart + dir * (t * 360f);

        // Elevation: low at dawn/dusk, high at midday (simple sine arc)
        float dayFactor = Mathf.Sin(t * Mathf.PI); // 0 at dawn/dusk, 1 at noon
        float elevation = Mathf.Lerp(elevationMin, elevationMax, dayFactor);

        Quaternion targetRot = Quaternion.Euler(elevation, azimuth, 0f);

        if (!enableSmoothing || !_seeded)
        {
            shadowLight.transform.rotation = targetRot;
            _seeded = true;
        }
        else
        {
            // Exponential smoothing factor so it’s framerate-independent
            float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothTime));
            shadowLight.transform.rotation = Quaternion.Slerp(shadowLight.transform.rotation, targetRot, k);
        }
        // === Shadow strength control (day nearly invisible, night as-is) ===
        if (controlShadowStrength && shadowLight != null)
        {
            float nightFactor;
            if (nightVolume != null)
            {
                nightFactor = Mathf.Clamp01(nightVolume.weight);
            }
            else
            {
                // Fallback: compute from TimeManager’s wedge (Sunset -> Sunrise) with feather
                float minutes = TimeManager.Instance.CurrentHourFloat * 60f;
                float start   = TimeManager.Instance.SunsetHour * 60f;
                float end     = TimeManager.Instance.SunriseHour * 60f;
                nightFactor = NightBlend(minutes, start, end, 1440f, Mathf.Max(1, fallbackFeatherMinutes));
            }

            float targetStrength = Mathf.Lerp(dayShadowStrength, nightShadowStrength, nightFactor);
            shadowLight.shadowStrength = targetStrength;
        }
    }

    private static float NightBlend(float x, float start, float end, float wrap, float feather)
    {
        // Inside test with wrap
        bool inside = (start < end) ? (x >= start && x < end) : (x >= start || x < end);

        // Distance to the nearest edge of the night interval
        float distToA = MinRingDistance(x, start, wrap);
        float distToB = MinRingDistance(x, end,   wrap);
        float d = Mathf.Min(distToA, distToB);

        float t = Mathf.Clamp01(d / Mathf.Max(0.0001f, feather));
        // Fade up before START, stay 1 inside (except near END), fade down after END
        return inside ? Mathf.SmoothStep(1f, 0f, t) : Mathf.SmoothStep(0f, 1f, t);
    }

    private static float MinRingDistance(float a, float b, float wrap)
    {
        float d = Mathf.Abs(a - b);
        return Mathf.Min(d, wrap - d);
    }
}