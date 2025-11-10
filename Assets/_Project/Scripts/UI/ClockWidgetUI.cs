using System.Collections;
using TMPro;
using UnityEngine;
using Game.Core.TimeSystem;

public class ClockWidgetUI : MonoBehaviour
{
    [Header("References (auto-wires common names)")]
    // Rotate the PARENT hinge (ArrowRoot). If not found, will fall back to a child named "Arrow".
    [SerializeField] private RectTransform arrow;   // ClockWidget/ArrowRoot (preferred) or ClockWidget/Arrow
    [SerializeField] private TMP_Text      dayNum;  // ClockWidget/DayNumber

    [Header("Rotation Mapping")]
    [Tooltip("Arrow rotation at MIDNIGHT (degrees). If your hand should point UP at midnight and your art points RIGHT at 0°, set this to 90.")]
    [SerializeField] private float angleAtMidnight = 90f;
    [Tooltip("Clockwise = -1, Counter-Clockwise = +1. Flip if spinning backward.")]
    [SerializeField] private int rotationDirection = +1;

    private bool subscribed;

    private void Awake()
    {
        // Prefer a centered parent hinge named "ArrowRoot"
        if (!arrow)
        {
            var root = transform.Find("ArrowRoot") as RectTransform;
            if (root != null) arrow = root;
        }
        // Fallback: allow direct child named "Arrow" if user didn't make ArrowRoot yet
        if (!arrow)
        {
            var img = transform.Find("Arrow") as RectTransform;
            if (img != null) arrow = img;
        }

        if (!dayNum)
            dayNum = transform.Find("DayNumber")?.GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (subscribed && TimeManager.Instance != null)
            TimeManager.Instance.OnDayStarted -= HandleDayStarted;
        subscribed = false;
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (TimeManager.Instance == null) yield return null;

        if (!subscribed)
        {
            TimeManager.Instance.OnDayStarted += HandleDayStarted;
            subscribed = true;
        }

        // initialize immediately
        HandleDayStarted(TimeManager.Instance.DayIndex);
        UpdateArrowImmediate();
    }

    private void Update()
    {
        if (!subscribed || TimeManager.Instance == null || arrow == null) return;

        float t = Mathf.Repeat(TimeManager.Instance.NormalizedDay, 1f); // 0..1 across the day
        float angle = angleAtMidnight + rotationDirection * (t * 360f);
        var e = arrow.localEulerAngles;
        e.z = angle;
        arrow.localEulerAngles = e;
    }

    private void HandleDayStarted(int dayIndex)
    {
        if (dayNum) dayNum.text = dayIndex.ToString();
    }

    private void UpdateArrowImmediate()
    {
        if (arrow == null || TimeManager.Instance == null) return;

        float t = Mathf.Repeat(TimeManager.Instance.NormalizedDay, 1f);
        float angle = angleAtMidnight + rotationDirection * (t * 360f);
        var e = arrow.localEulerAngles;
        e.z = angle;
        arrow.localEulerAngles = e;
    }
}