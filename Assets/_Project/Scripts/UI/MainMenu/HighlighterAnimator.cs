using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Attach this to the button's Highlight object (the Image under the button).
[RequireComponent(typeof(RectTransform))]
public class HighlighterAnimator : MonoBehaviour
{
    RectTransform rt;
    Image img;
    Coroutine animCo;
    Coroutine mirrorCo;

    [Tooltip("Average seconds between subtle automatic mirror flips (lower = faster shimmer)")]
    public float mirrorPeriod = 0.8f;   // default speed you liked

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        ResetState();
    }

    void OnEnable()
    {
        // Start or restart the continuous mirroring whenever highlight becomes active
        if (mirrorCo == null)
            mirrorCo = StartCoroutine(MirrorLoop(mirrorPeriod));
    }

    void OnDisable()
    {
        if (animCo != null) { StopCoroutine(animCo); animCo = null; }
        if (mirrorCo != null) { StopCoroutine(mirrorCo); mirrorCo = null; }
        ResetState();
    }

    public void PlayRandom()
    {
        if (!gameObject.activeInHierarchy) return;
        if (animCo != null) StopCoroutine(animCo);

        float r = Random.value;
        if (r < 0.10f)      animCo = StartCoroutine(FlipOnce(0.14f));                // 10%
        else if (r < 0.45f) animCo = StartCoroutine(StretchTall(0.18f, 1.18f));      // 35%
        else                animCo = StartCoroutine(BounceScale(0.16f));             // 55%
    }

    public void ResetState()
    {
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x, rt.anchoredPosition3D.y, 0f);
    }

    // --- Continuous idle shimmer ---
    IEnumerator MirrorLoop(float period)
    {
        // slight random start offset so multiple highlights aren’t perfectly synced
        yield return new WaitForSecondsRealtime(Random.Range(0f, period * 0.25f));

        while (true)
        {
            // flip vertically by inverting Y scale
            Vector3 s = rt.localScale;
            s.y *= -1f;
            rt.localScale = s;

            // very subtle random variation (~±0.05s)
            float wait = Mathf.Max(0.1f, period + Random.Range(-0.05f, 0.05f));
            yield return new WaitForSecondsRealtime(wait);
        }
    }

    // --- Button-triggered micro animations ---
    IEnumerator BounceScale(float dur)
    {
        float t = 0f;
        Vector3 a = new(0.92f, 0.92f, 1f);
        Vector3 b = new(1.06f, 1.06f, 1f);
        Vector3 c = Vector3.one;

        while (t < dur * 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float u = EaseOutQuad(t / (dur * 0.5f));
            rt.localScale = Vector3.LerpUnclamped(a, b, u);
            yield return null;
        }
        t = 0f;
        while (t < dur * 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float u = EaseOutQuad(t / (dur * 0.5f));
            rt.localScale = Vector3.LerpUnclamped(b, c, u);
            yield return null;
        }
        rt.localScale = Vector3.one;
        animCo = null;
    }

    IEnumerator StretchTall(float dur, float yScale)
    {
        float t = 0f;
        Vector3 start = Vector3.one;
        Vector3 peak  = new(1f, yScale, 1f);
        while (t < dur * 0.6f)
        {
            t += Time.unscaledDeltaTime;
            float u = EaseOutQuad(t / (dur * 0.6f));
            rt.localScale = Vector3.LerpUnclamped(start, peak, u);
            yield return null;
        }
        t = 0f;
        while (t < dur * 0.4f)
        {
            t += Time.unscaledDeltaTime;
            float u = EaseOutQuad(t / (dur * 0.4f));
            rt.localScale = Vector3.LerpUnclamped(peak, Vector3.one, u);
            yield return null;
        }
        rt.localScale = Vector3.one;
        animCo = null;
    }

    IEnumerator FlipOnce(float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = EaseOutQuad(t / dur);
            float angle = Mathf.LerpUnclamped(0f, 180f, u);
            rt.localRotation = Quaternion.Euler(angle, 0f, 0f);
            yield return null;
        }

        rt.localRotation = Quaternion.Euler(180f, 0f, 0f);
        rt.localScale = Vector3.one;
        animCo = null;
    }

    static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
}