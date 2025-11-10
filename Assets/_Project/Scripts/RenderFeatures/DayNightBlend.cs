using UnityEngine;
using UnityEngine.Rendering;
using Game.Core.TimeSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class DayNightBlend : MonoBehaviour
{
    [Header("Scene Refs")]
    [SerializeField] private Volume nightVolume;     // GlobalVolume_Night (weight will be driven)
    [SerializeField] private Light  sunLight;        // Directional Light "Sunlight"

    [Header("Night Window (red wedge)")]
    [SerializeField] private bool followTimeManagerWedge = true; // if true, use TimeManager.SunsetHour -> SunriseHour
    [SerializeField, Range(0f,23.99f)] private float nightStartHour = 18f;  // manual fallback when not following
    [SerializeField, Range(0f,23.99f)] private float nightEndHour   = 6f;   // manual fallback when not following
    [SerializeField, Range(0f,180f)]   private float featherMinutes = 60f;  // softness at edges
    [SerializeField, Range(0f,5f)]     private float boundaryEpsilonMinutes = 0.1f; // expands night window ±epsilon to avoid one-frame flips (0.1 min = 6 sec)

    [Header("Sun Light")]
    [SerializeField] private float dayIntensity = 1.2f;
    [SerializeField] private float nightIntensity = 0.3f;
    [SerializeField] private Color dayColor   = new(1.00f, 0.96f, 0.90f);
    [SerializeField] private Color nightColor = new(0.55f, 0.65f, 1.00f);

    [Header("Smoothing")]
    [SerializeField] private bool  enableSmoothing   = true;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.35f;
    [SerializeField] private bool  useUnscaledTime   = false;

    private float _wSmoothed = 0f;
    private float _wVel      = 0f;
    private bool  _seeded    = false;

    private void Reset()
    {
        if (!sunLight)    sunLight    = RenderSettings.sun;
    }

    private void OnEnable()
    {
        _seeded = false;
    }

    private void Update()
    {
        if (TimeManager.Instance == null) return;

        // Determine start/end hours without mutating serialized fields
        float startHour = followTimeManagerWedge ? TimeManager.Instance.SunsetHour  : nightStartHour;
        float endHour   = followTimeManagerWedge ? TimeManager.Instance.SunriseHour : nightEndHour;

        float hourNow = TimeManager.Instance.CurrentHourFloat; // fractional hour
        float minutes = hourNow * 60f;
        float start   = startHour * 60f;
        float end     = endHour   * 60f;

        float w = NightWeight(minutes, start, end, 1440f, featherMinutes, boundaryEpsilonMinutes);

        // Temporal smoothing (optional) to avoid frame-to-frame stepping
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (!_seeded)
        {
            _wSmoothed = w; // seed on first frame to avoid pop
            _seeded = true;
        }
        else if (enableSmoothing)
        {
            _wSmoothed = Mathf.SmoothDamp(_wSmoothed, w, ref _wVel, smoothTime, Mathf.Infinity, Mathf.Max(0f, dt));
        }
        else
        {
            _wSmoothed = w;
        }

        if (nightVolume) nightVolume.weight = _wSmoothed;

        if (sunLight)
        {
            sunLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, _wSmoothed);
            sunLight.color     = Color.Lerp(dayColor, nightColor, _wSmoothed);
        }
    }

    // Compute 0..1 blend with wrap-around, feather, and epsilon, with asymmetric behavior:
    // - Outside before START: ramp 0→1 as you approach START within feather.
    // - Inside: stay at 1, except ramp 1→0 as you approach END within feather.
    private static float NightWeight(float x, float start, float end, float wrap, float feather, float epsilon)
    {
        feather = Mathf.Max(0.0001f, feather);
        epsilon = Mathf.Max(0f, epsilon);

        // Distances on ring
        float toStartFwd = ForwardDist(x, start, wrap); // ahead to START
        float toEndFwd   = ForwardDist(x, end,   wrap); // ahead to END

        // Epsilon-expanded inside test
        float s0 = start - epsilon;
        float e0 = end   + epsilon;
        bool inside = (s0 < e0) ? (x >= s0 && x < e0)
                                : (x >= s0 || x < e0);

        if (inside)
        {
            // Fade OUT only when approaching END (last `feather` minutes)
            float tEnd = Mathf.Clamp01((feather - Mathf.Max(0f, toEndFwd - epsilon)) / feather);
            return Mathf.SmoothStep(1f, 0f, tEnd);
        }
        else
        {
            // Fade IN only when approaching START (last `feather` minutes before START)
            float tStart = Mathf.Clamp01((feather - Mathf.Max(0f, toStartFwd - epsilon)) / feather);
            return Mathf.SmoothStep(0f, 1f, tStart);
        }
    }

    private static float MinRingDistance(float a, float b, float wrap)
    {
        float d = Mathf.Abs(a - b);
        return Mathf.Min(d, wrap - d);
    }

    private static float ForwardDist(float from, float to, float wrap)
    {
        float d = to - from;
        if (d < 0f) d += wrap;
        return d;
    }

    private void OnValidate()
    {
        nightStartHour = Mathf.Clamp(nightStartHour, 0f, 23.99f);
        nightEndHour   = Mathf.Clamp(nightEndHour,   0f, 23.99f);
        featherMinutes = Mathf.Clamp(featherMinutes, 0f, 180f);
        boundaryEpsilonMinutes = Mathf.Clamp(boundaryEpsilonMinutes, 0f, 5f);
    }
}