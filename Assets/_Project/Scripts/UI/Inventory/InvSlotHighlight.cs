using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Selectable))]
public class InvSlotHighlight : MonoBehaviour,
    ISelectHandler, IDeselectHandler,
    IPointerClickHandler, ISubmitHandler
{
    [Header("Visual References")]
    [SerializeField] private Image borderImage;   // assign the Border child in prefab

    [Header("Highlight Settings")]
    [Tooltip("How much larger the slot becomes when selected (1.1 = 10% bigger).")]
    [SerializeField] private float scaleMultiplier = 1.08f;
    [Tooltip("Seconds to scale up/down.")]
    [SerializeField] private float scaleSpeed = 0.10f;
    [Tooltip("Border color when selected.")]
    [SerializeField] private Color borderColor = new Color(1f, 0.3f, 0.3f, 1f);
    [Tooltip("Border width (for 9-sliced sprite).")]
    [SerializeField, Range(0f, 20f)] private float borderSize = 4f;

    private Vector3 _originalScale;
    private Coroutine _scaleRoutine;
    private bool _isSelected;

    void Awake()
    {
        _originalScale = transform.localScale;
        if (borderImage != null)
        {
            borderImage.enabled = false;
            ApplyBorderSize();
        }
    }

    // Selection persistence is based ONLY on selection state (not hover)
    public void OnSelect(BaseEventData eventData)  => SetSelected(true);
    public void OnDeselect(BaseEventData eventData) => SetSelected(false);

    // Clicking or gamepad submit will force-select this slot and keep it selected
    public void OnPointerClick(PointerEventData eventData)
    {
        EventSystem.current?.SetSelectedGameObject(gameObject);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        EventSystem.current?.SetSelectedGameObject(gameObject);
        // Optional: trigger a confirm pulse or sound here later
        StartCoroutine(Pulse());
    }

    private void SetSelected(bool selected)
    {
        if (_isSelected == selected) return;
        _isSelected = selected;

        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(AnimateScale(selected));

        if (borderImage != null)
        {
            borderImage.color = borderColor;
            borderImage.enabled = selected;
        }
    }

    private System.Collections.IEnumerator AnimateScale(bool grow)
    {
        float t = 0f;
        Vector3 start = transform.localScale;
        Vector3 target = grow ? _originalScale * scaleMultiplier : _originalScale;
        float dur = Mathf.Max(0.0001f, scaleSpeed);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            transform.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        transform.localScale = target;
    }

    void OnValidate()
    {
        ApplyBorderSize();
    }

    void OnDisable()
    {
        if (_scaleRoutine != null)
        {
            StopCoroutine(_scaleRoutine);
            _scaleRoutine = null;
        }

        transform.localScale = _originalScale;
        _isSelected = false;

        if (borderImage != null)
            borderImage.enabled = false;
    }

    private System.Collections.IEnumerator Pulse()
    {
        const float pulseTime = 0.12f;
        float t = 0f;
        Vector3 start = transform.localScale;
        Vector3 peak = start * 1.05f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / pulseTime;
            transform.localScale = Vector3.Lerp(start, peak, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        transform.localScale = start;
    }

    private void ApplyBorderSize()
    {
        // Simple 9-slice thickness control without custom shader.
        if (borderImage && borderImage.type == Image.Type.Sliced)
        {
            borderImage.pixelsPerUnitMultiplier = Mathf.Max(0.1f, 10f / Mathf.Max(1f, borderSize));
        }
    }
}