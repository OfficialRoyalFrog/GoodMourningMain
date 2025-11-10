using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class FireflyTail : MonoBehaviour
{
    public enum Ease { Linear, Smooth, EaseIn, EaseOut, Power }

    [Header("Width (meters)")]
    [Min(0f)] public float startWidth = 0.08f;   // at the head
    [Min(0f)] public float endWidth   = 0.00f;   // at the tail

    [Header("Taper Shape")]
    public Ease taper = Ease.Smooth;
    [Range(0.25f,4f)] public float power = 1.5f; // used when taper = Power

    [Header("Apply To")]
    [SerializeField] private TrailRenderer target; // auto-finds if null

    AnimationCurve _curve;

    void OnEnable()            { Apply(); }
    void OnValidate()          { Apply(); }
    void Reset()               { target = GetComponent<TrailRenderer>(); }

    void Apply()
    {
        if (!target) target = GetComponent<TrailRenderer>();
        if (!target) return;

        // Build a 2-key curve from start→end
        // We’ll shape it by adjusting tangents or inserting mid key if needed.
        switch (taper)
        {
            case Ease.Linear:
                _curve = new AnimationCurve(
                    new Keyframe(0f, startWidth),
                    new Keyframe(1f, endWidth)
                );
                break;

            case Ease.Smooth:
                _curve = new AnimationCurve(
                    new Keyframe(0f, startWidth),
                    new Keyframe(1f, endWidth)
                );
                _curve.SmoothTangents(0, 0f);
                _curve.SmoothTangents(1, 0f);
                break;

            case Ease.EaseIn:    // skinny tail; strong fall near the end
                _curve = new AnimationCurve(
                    new Keyframe(0f, startWidth),
                    new Keyframe(0.7f, Mathf.Lerp(startWidth, endWidth, 0.5f)),
                    new Keyframe(1f, endWidth)
                );
                _curve.SmoothTangents(0, 0f);
                _curve.SmoothTangents(1, 0f);
                break;

            case Ease.EaseOut:   // skinny near the head; quick drop early
                _curve = new AnimationCurve(
                    new Keyframe(0f, startWidth),
                    new Keyframe(0.3f, Mathf.Lerp(startWidth, endWidth, 0.5f)),
                    new Keyframe(1f, endWidth)
                );
                _curve.SmoothTangents(0, 0f);
                _curve.SmoothTangents(1, 0f);
                break;

            case Ease.Power:     // y = start*(1-x)^p + end*x^p style taper
                // sample a few points to approximate a power curve
                int samples = 5;
                Keyframe[] ks = new Keyframe[samples];
                for (int i = 0; i < samples; i++)
                {
                    float x = i / (samples - 1f);          // 0..1
                    float w = Mathf.Lerp(startWidth, endWidth, Mathf.Pow(x, power));
                    ks[i] = new Keyframe(x, w);
                }
                _curve = new AnimationCurve(ks);
                for (int i = 0; i < _curve.length; i++) _curve.SmoothTangents(i, 0f);
                break;
        }

        target.widthCurve = _curve;
    }
}