using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TrailColorSimple : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private TrailRenderer target;     // assign explicitly or leave null to auto-find
    [SerializeField] private bool findInChildren = true;

    [Header("Colors (HDR ok)")]
    [ColorUsage(true,true)] public Color headColor = Color.white;
    [ColorUsage(true,true)] public Color tailColor = new Color(1f,1f,1f,0f);

    [Header("Alpha")]
    [Range(0f,1f)] public float headAlpha = 0.6f;
    [Range(0f,1f)] public float tailAlpha = 0.0f;

    [Header("Apply")]
    [SerializeField] private bool applyOnEnable   = true;
    [SerializeField] private bool applyOnValidate = true;
    [SerializeField] private bool applyEveryFrame = false; // turn on only while debugging

    void Reset() { ResolveTarget(); }
    void OnEnable() { if (applyOnEnable) Apply(); }
    void OnValidate() { if (applyOnValidate) Apply(); }
    void Update() { if (applyEveryFrame) Apply(); }

    void ResolveTarget()
    {
        if (target) return;
        if (findInChildren)
            target = GetComponentInChildren<TrailRenderer>(true);
        else
            target = GetComponent<TrailRenderer>();
    }

    public void Apply()
    {
        ResolveTarget();
        if (!target) return;

        // Build a simple 2-key gradient using your head/tail colors & alphas
        var g = new Gradient();
        var colors = new GradientColorKey[2];
        var alphas = new GradientAlphaKey[2];

        colors[0] = new GradientColorKey(new Color(headColor.r, headColor.g, headColor.b, 1f), 0f);
        colors[1] = new GradientColorKey(new Color(tailColor.r, tailColor.g, tailColor.b, 1f), 1f);

        alphas[0] = new GradientAlphaKey(headAlpha, 0f);
        alphas[1] = new GradientAlphaKey(tailAlpha, 1f);

        g.SetKeys(colors, alphas);
        target.colorGradient = g;
    }
}