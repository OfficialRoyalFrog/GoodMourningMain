using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToastUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private RectTransform rt;            // ToastItem
    [SerializeField] private Image bgBottom;              // Toast 2 (50% opacity)
    [SerializeField] private Image bgTop;                 // Toast 1 (100%)
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI labelMain;   // "+10 Stone"
    [SerializeField] private TextMeshProUGUI labelTotal;  // "172"

    [Header("Positioning")]
    [Tooltip("X position when fully visible, relative to parent (usually a small left margin).")]
    public float targetX = 24f;
    [Tooltip("How far to start off-screen on the left (px).")]
    public float startOffsetLeft = 900f;

    [Header("Enter/Bounce (unscaled)")]
    public float enterTime = 0.25f;
    public float bounceTime = 0.10f;
    public float overshootPx = 8f;
    public AnimationCurve enterCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Hold")]
    public float holdSeconds = 1.6f;

    [Header("Exit (unscaled)")]
    public float nudgeRightPx = 12f;
    public float nudgeTime = 0.08f;
    public float exitTime = 0.28f;
    public float exitOffsetLeft = 1000f;
    public AnimationCurve nudgeCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public AnimationCurve exitCurve = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Visual")]
    [Range(0f,1f)] public float bottomOpacity = 0.5f;
    public Color mainTextColor = Color.white;
    public Color amountColor = new Color(1f,0.92f,0.4f);
    public Color totalTextColor = new Color(1f,1f,1f,0.6f);

    // runtime
    private bool exiting;
    private string displayName;
    private int runningAdd;
    private int totalCount;

    void Reset()
    {
        cg = GetComponent<CanvasGroup>();
        rt = GetComponent<RectTransform>();
        if (!bgBottom) bgBottom = transform.Find("BgBottom")?.GetComponent<Image>();
        if (!bgTop) bgTop = transform.Find("BgTop")?.GetComponent<Image>();
        if (!icon) icon = transform.Find("Row/Icon")?.GetComponent<Image>();
        if (!labelMain) labelMain = transform.Find("Row/TextRow/LabelMain")?.GetComponent<TextMeshProUGUI>();
        if (!labelTotal) labelTotal = transform.Find("Row/TextRow/LabelTotal")?.GetComponent<TextMeshProUGUI>();
    }

    public void Setup(Sprite iconSprite, string itemDisplayName, int addedAmount, int newTotal)
    {
        displayName = itemDisplayName;
        runningAdd = addedAmount;
        totalCount = newTotal;

        if (icon) icon.sprite = iconSprite;

        if (bgBottom)
        {
            var c = bgBottom.color; c.a = bottomOpacity; bgBottom.color = c;
            bgBottom.type = Image.Type.Sliced;
        }
        if (bgTop) bgTop.type = Image.Type.Sliced;

        ApplyText();
        StartSequence();
    }

    public void MergeAmount(int delta, int updatedTotal, float extraHold = 0.6f)
    {
        runningAdd += delta;
        totalCount = updatedTotal;
        ApplyText();
        holdSeconds += extraHold;
        if (exiting)
        {
            exiting = false;
            StopAllCoroutines();
            StartSequence();
        }
    }

    public void SetY(float y)
    {
        // keep X where it is, set Y slot exactly
        var p = rt.anchoredPosition;
        rt.anchoredPosition = new Vector2(p.x, y);
    }

    public float Height => rt.rect.height;

    public void ForceEarlyExit(float speedMult = 1.6f)
    {
        if (exiting) return;
        nudgeTime /= speedMult;
        exitTime  /= speedMult;
        holdSeconds = 0f; // skip to exit
    }

    private void ApplyText()
    {
        if (labelMain)
        {
            labelMain.richText = true;
            labelMain.color = mainTextColor;
            labelMain.text = $"<color=#{ColorUtility.ToHtmlStringRGB(amountColor)}>+{runningAdd}</color> {displayName}";
        }
        if (labelTotal)
        {
            labelTotal.color = totalTextColor;
            labelTotal.text = totalCount.ToString();
        }
    }

    private void StartSequence()
    {
        StopAllCoroutines();
        StartCoroutine(Co_Run());
    }

    private IEnumerator Co_Run()
    {
        cg.alpha = 1f;
        // start off-screen (left)
        var pos = rt.anchoredPosition;
        rt.anchoredPosition = new Vector2(-startOffsetLeft, pos.y);

        // ENTER: slide to overshoot
        float t = 0f;
        Vector2 start = rt.anchoredPosition;
        Vector2 overshoot = new Vector2(targetX + overshootPx, pos.y);
        while (t < enterTime)
        {
            float k = enterCurve.Evaluate(t / enterTime);
            rt.anchoredPosition = Vector2.LerpUnclamped(start, overshoot, k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // BOUNCE to rest (targetX)
        t = 0f;
        Vector2 rest = new Vector2(targetX, pos.y);
        while (t < bounceTime)
        {
            float k = bounceCurve.Evaluate(t / bounceTime);
            rt.anchoredPosition = Vector2.LerpUnclamped(overshoot, rest, k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        rt.anchoredPosition = rest;

        // HOLD
        t = 0f;
        while (t < holdSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // EXIT: nudge right then slide left off-screen
        exiting = true;
        t = 0f;
        Vector2 nudgeStart = rt.anchoredPosition;
        Vector2 nudgeEnd   = new Vector2(targetX + nudgeRightPx, nudgeStart.y);
        while (t < nudgeTime)
        {
            float k = nudgeCurve.Evaluate(t / nudgeTime);
            rt.anchoredPosition = Vector2.LerpUnclamped(nudgeStart, nudgeEnd, k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
        Vector2 outStart = rt.anchoredPosition;
        Vector2 outEnd   = new Vector2(-exitOffsetLeft, outStart.y);
        while (t < exitTime)
        {
            float k = exitCurve.Evaluate(t / exitTime);
            rt.anchoredPosition = Vector2.LerpUnclamped(outStart, outEnd, k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}