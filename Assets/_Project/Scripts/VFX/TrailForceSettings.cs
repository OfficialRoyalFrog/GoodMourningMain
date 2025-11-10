using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TrailForceSettings : MonoBehaviour
{
    [Header("Apply To")]
    [SerializeField] bool includeInactive = true;
    [SerializeField] bool applyEveryFrame = true; // turn on while testing

    [Header("Width (meters)")]
    [Min(0f)] public float startWidth = 0.06f;
    [Min(0f)] public float endWidth   = 0.00f;

    [Header("Alpha Fade")]
    [Range(0f,1f)] public float headAlpha = 0.60f;
    [Range(0f,1f)] public float tailAlpha = 0.00f;

    [Header("Color (optional tint)")]
    [ColorUsage(true,true)] public Color headColor = Color.white;
    [ColorUsage(true,true)] public Color tailColor = Color.white;

    int _lastAppliedCount = -1;

    void OnEnable()   { Apply(); }
    void OnValidate() { Apply(); }
    void Update()     { if (applyEveryFrame) Apply(); }

    [ContextMenu("Apply Now")]
    public void Apply()
    {
        var trails = GetComponentsInChildren<TrailRenderer>(includeInactive);
        foreach (var tr in trails)
        {
            // WIDTH
            var curve = new AnimationCurve(
                new Keyframe(0f, startWidth),
                new Keyframe(1f, endWidth)
            );
            curve.SmoothTangents(0, 0f);
            curve.SmoothTangents(1, 0f);
            tr.widthCurve = curve;

            // COLOR/ALPHA
            var g = new Gradient();
            var colors = new GradientColorKey[2];
            var alphas = new GradientAlphaKey[2];
            colors[0] = new GradientColorKey(new Color(headColor.r, headColor.g, headColor.b, 1f), 0f);
            colors[1] = new GradientColorKey(new Color(tailColor.r, tailColor.g, tailColor.b, 1f), 1f);
            alphas[0] = new GradientAlphaKey(headAlpha, 0f);
            alphas[1] = new GradientAlphaKey(tailAlpha, 1f);
            g.SetKeys(colors, alphas);
            tr.colorGradient = g;
        }

        if (_lastAppliedCount != trails.Length)
        {
            _lastAppliedCount = trails.Length;
#if UNITY_EDITOR
            Debug.Log($"[TrailForceSettings] Applied to {trails.Length} TrailRenderer(s) under '{name}'.");
#endif
        }
    }
}