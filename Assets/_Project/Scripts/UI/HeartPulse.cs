using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class HeartPulse : MonoBehaviour
{
    [Header("Beat Shape")]
    [SerializeField, Range(0.0f, 0.3f)] float firstBeatScale = 0.04f;
    [SerializeField, Range(0.0f, 0.3f)] float secondBeatScale = 0.08f;
    [SerializeField, Range(0.01f, 0.4f)] float firstBeatDuration = 0.08f;
    [SerializeField, Range(0.01f, 0.4f)] float secondBeatDuration = 0.10f;

    [Header("Timing")]
    [SerializeField, Range(0.0f, 0.5f)] float betweenBeatDelay = 0.05f;
    [SerializeField, Range(0.0f, 1.0f)] float restDelay = 0.35f;
    [SerializeField] bool useUnscaledTime = true;

    RectTransform rectTransform;
    Vector3 baseScale = Vector3.one;
    Coroutine pulseRoutine;
    bool pulsing;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        if (rectTransform)
            baseScale = rectTransform.localScale;
    }

    void OnEnable()
    {
        if (!rectTransform)
            rectTransform = transform as RectTransform;
        ResetScale();
        TryStartPulse();
    }

    void OnDisable()
    {
        StopPulseRoutine();
        pulsing = false;
        ResetScale();
    }

    public void SetPulsing(bool shouldPulse)
    {
        if (pulsing == shouldPulse)
            return;

        pulsing = shouldPulse;
        if (pulsing)
            TryStartPulse();
        else
            StopPulseRoutine();
    }

    void TryStartPulse()
    {
        if (!pulsing || pulseRoutine != null || !rectTransform)
            return;
        pulseRoutine = StartCoroutine(PulseLoop());
    }

    void StopPulseRoutine()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
        ResetScale();
    }

    System.Collections.IEnumerator PulseLoop()
    {
        while (pulsing && rectTransform)
        {
            yield return Beat(firstBeatScale, firstBeatDuration);
            yield return Wait(betweenBeatDelay);
            yield return Beat(secondBeatScale, secondBeatDuration);
            yield return Wait(restDelay);
        }
        pulseRoutine = null;
    }

    System.Collections.IEnumerator Beat(float amplitude, float duration)
    {
        if (duration <= 0f || amplitude <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += DeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * amplitude;
            rectTransform.localScale = baseScale * scale;
            yield return null;
        }
        rectTransform.localScale = baseScale;
    }

    System.Collections.IEnumerator Wait(float duration)
    {
        if (duration <= 0f)
            yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += DeltaTime();
            yield return null;
        }
    }

    float DeltaTime() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    void ResetScale()
    {
        if (rectTransform)
            rectTransform.localScale = baseScale;
    }
}
