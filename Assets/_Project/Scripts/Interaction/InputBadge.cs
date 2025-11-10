using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class InputBadge : MonoBehaviour
{
    public PlayerInput playerInput;     // drag your Player (with PlayerInput)
    public string actionName = "Interact";
    public TextMeshProUGUI label;       // InputLabel TMP
    public Image background;            // InputBadge Image
    public Vector2 circleSize = new Vector2(40, 40); // base size

    void Awake()
    {
        if (background) {
            var rt = background.rectTransform;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, circleSize.x);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   circleSize.y);
        }
        UpdateBadge();
    }

    void OnEnable()
    {
        if (playerInput) playerInput.onControlsChanged += OnControlsChanged;
        UpdateBadge();
    }

    void OnDisable()
    {
        if (playerInput) playerInput.onControlsChanged -= OnControlsChanged;
    }

    void OnControlsChanged(PlayerInput _)
    {
        UpdateBadge();
    }

    public void UpdateBadge()
    {
        if (!label) return;
        string token = InputHintUtility.GetShortBinding(playerInput, actionName);
        label.text = token;
    }
}