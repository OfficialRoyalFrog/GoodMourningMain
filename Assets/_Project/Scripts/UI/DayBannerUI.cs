using System.Collections;
using TMPro;
using UnityEngine;
using Game.Core.TimeSystem;

public class DayBannerUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup group; // controls alpha (auto-wired in Awake if null)
    [SerializeField] private TMP_Text    label; // the "Day N" text (auto-wired in Awake if null)

    [Header("Timings (seconds)")]
    [SerializeField] private float fadeSeconds = 0.5f;
    [SerializeField] private float holdSeconds = 0.5f;

    [Header("Behavior")]
    [Tooltip("Show current day on scene load even if TimeManager initializes later.")]
    [SerializeField] private bool showDayOnStart = true;

    private Coroutine current;
    private bool subscribed;

    private void Awake()
    {
        // Auto-wire references if not assigned in Inspector
        if (!group) group = GetComponent<CanvasGroup>();
        if (!label) label = GetComponentInChildren<TMP_Text>(true);

        // Ensure we start hidden; coroutine will animate visibility
        if (group) group.alpha = 0f;
    }

    private void OnEnable()
    {
        // Resilient subscription that waits for TimeManager to exist
        StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Wait until TimeManager is alive; covers init-order differences
        while (TimeManager.Instance == null)
            yield return null;

        if (!subscribed)
        {
            TimeManager.Instance.OnDayStarted += HandleDayStarted;
            subscribed = true;
        }

        // Show current day immediately (e.g., Day 1) so banner appears on load
        if (showDayOnStart && TimeManager.Instance.DayIndex >= 1)
        {
            HandleDayStarted(TimeManager.Instance.DayIndex);
        }
    }

    private void Unsubscribe()
    {
        if (subscribed && TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStarted -= HandleDayStarted;
        }
        subscribed = false;
    }

    private void HandleDayStarted(int dayIndex)
    {
        if (label) label.text = $"Day {dayIndex}";
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        if (group == null) yield break; // safety guard

        group.blocksRaycasts = false;

        float fade = Mathf.Max(0.0001f, fadeSeconds);

        // Fade in (unscaled so pause at midnight doesn't freeze the banner)
        for (float t = 0; t < fade; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / fade;
            yield return null;
        }
        group.alpha = 1f;

        // Hold fully visible
        float timer = 0f;
        while (timer < holdSeconds)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out
        for (float t = 0; t < fade; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - (t / fade);
            yield return null;
        }
        group.alpha = 0f;
    }
}