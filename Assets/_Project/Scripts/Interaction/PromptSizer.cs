using UnityEngine;
using TMPro;
using UnityEngine.UI;

[ExecuteAlways]
public class PromptSizer : MonoBehaviour
{
    [Header("Row Parts")]
    public RectTransform promptRow;      // PromptRow (Horizontal Layout Group)
    public RectTransform badge;          // InputBadge (Image) RectTransform (optional)
    public TextMeshProUGUI text;         // PromptText (TMP)

    [Header("Stroke Padding (inside the black bar)")]
    [Min(0f)] public float horizontalPadding = 48f; // total (left + right) inside stroke

    [Header("Clamp")]
    [Min(0f)] public float minWidth = 160f;
    [Min(0f)] public float maxWidth = 900f;

    // Internals
    RectTransform self;                  // PromptStroke RectTransform
    HorizontalLayoutGroup rowLayout;

    void Awake()
    {
        self = GetComponent<RectTransform>();
        CacheRowLayout();
    }

    void OnValidate()
    {
        if (!self) self = GetComponent<RectTransform>();
        CacheRowLayout();
    }

    void CacheRowLayout()
    {
        if (promptRow)
            rowLayout = promptRow.GetComponent<HorizontalLayoutGroup>();
        else
            rowLayout = null;
    }

    void LateUpdate()
    {
        if (!self || !text) return;

        // --- 1) widths ---
        float textW = Mathf.Max(0f, text.preferredWidth);

        float badgeW = 0f;
        if (badge && badge.gameObject.activeInHierarchy)
        {
            badgeW = badge.rect.width;
            if (badgeW <= 0f) // if not laid out yet, fall back to preferred
                badgeW = LayoutUtility.GetPreferredWidth(badge);
        }

        // --- 2) spacing + row padding ---
        float spacing = rowLayout ? rowLayout.spacing : 0f;
        int padL = rowLayout ? rowLayout.padding.left : 0;
        int padR = rowLayout ? rowLayout.padding.right : 0;

        // only add spacing when the badge is visible
        float pairSpacing = (badgeW > 0f) ? spacing : 0f;

        // --- 3) total width inside stroke ---
        float total = badgeW + pairSpacing + textW + padL + padR + horizontalPadding;
        total = Mathf.Clamp(total, minWidth, maxWidth);

        // apply to stroke
        self.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, total);

        // --- 4) keep the row centered in the stroke (prevents badge drifting) ---
        if (promptRow)
        {
            // force center anchors/pivot each frame in case editor tweaks override them
            promptRow.anchorMin = new Vector2(0.5f, 0.5f);
            promptRow.anchorMax = new Vector2(0.5f, 0.5f);
            promptRow.pivot     = new Vector2(0.5f, 0.5f);
            promptRow.anchoredPosition = Vector2.zero;
        }

        // height: keep from sliced sprite; do not modify
    }
}