using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public sealed class DeleteSelectUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject slotSelectPanel;   // reference to Panel_SlotSelect (to return to)
    [SerializeField] private GameObject confirmPanel;      // reference to Panel_DeleteConfirm (full-screen confirm)

    [Header("Buttons")]
    [SerializeField] private Button btnSlot1;
    [SerializeField] private Button btnSlot2;
    [SerializeField] private Button btnSlot3;
    [SerializeField] private Button btnBack;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI lblSlot1;
    [SerializeField] private TextMeshProUGUI lblSlot2;
    [SerializeField] private TextMeshProUGUI lblSlot3;

    [Header("Optional")]
    [Header("Focus")]
    [SerializeField] private Button firstFocus; // Optional: set in Inspector (e.g., Btn_Slot1)
    [SerializeField] private TextMeshProUGUI selectedLabel; // optional readout

    private int selectedSlot = 1;
    private bool[] occupied = new bool[SaveSlots.MaxSlots + 1]; // 1..3

    void Awake()
    {
        if (btnSlot1) btnSlot1.onClick.AddListener(() => OnClickSlot(1));
        else Debug.LogWarning("[DeleteSelectUI] btnSlot1 not assigned.");

        if (btnSlot2) btnSlot2.onClick.AddListener(() => OnClickSlot(2));
        else Debug.LogWarning("[DeleteSelectUI] btnSlot2 not assigned.");

        if (btnSlot3) btnSlot3.onClick.AddListener(() => OnClickSlot(3));
        else Debug.LogWarning("[DeleteSelectUI] btnSlot3 not assigned.");

        if (btnBack) btnBack.onClick.AddListener(OnClickBack);
        else Debug.LogWarning("[DeleteSelectUI] btnBack not assigned.");
    }

    public void Show()
    {
        if (slotSelectPanel) slotSelectPanel.SetActive(false);
        else Debug.LogWarning("[DeleteSelectUI] slotSelectPanel not assigned.");

        Refresh();
        gameObject.SetActive(true);

        // Queue focus to first button for controller/keyboard navigation
        QueueFocusFirstButton();
    }

    public void Refresh()
    {
        RefreshSlot(1, lblSlot1);
        RefreshSlot(2, lblSlot2);
        RefreshSlot(3, lblSlot3);
        SelectSlot(1);
    }

    void RefreshSlot(int slot, TextMeshProUGUI label)
    {
        try
        {
            occupied[slot] = SaveSlots.TryReadSummary(slot, out var s);

            if (!label)
            {
                Debug.LogWarning($"[DeleteSelectUI] Label for slot {slot} not assigned.");
                return;
            }

            if (!occupied[slot]) { label.text = "---"; return; }

            string hh = s.hour.ToString("D2");
            string mm = s.minute.ToString("D2");
            label.text = $"Day {s.day}   {hh}:{mm}";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DeleteSelectUI] Error refreshing slot {slot}: {e.Message}");
        }
    }

    void SelectSlot(int slot)
    {
        selectedSlot = Mathf.Clamp(slot, 1, SaveSlots.MaxSlots);
        if (selectedLabel)
            selectedLabel.text = $"Selected: Slot {selectedSlot}";
    }

    void OnClickSlot(int slot)
    {
        Debug.Log($"[DeleteSelectUI] Clicked slot {slot}");
        SelectSlot(slot);

        if (slot < 1 || slot > SaveSlots.MaxSlots)
        {
            Debug.LogError($"[DeleteSelectUI] Invalid slot index {slot}. Max allowed: {SaveSlots.MaxSlots}");
            return;
        }

        if (!occupied[slot])
        {
            Debug.Log($"[DeleteSelectUI] Slot {slot} is empty — nothing to delete.");
            return;
        }

        if (!confirmPanel)
        {
            Debug.LogError("[DeleteSelectUI] Confirm panel not assigned.");
            return;
        }

        var confirm = confirmPanel.GetComponent<DeleteConfirmUI>();
        if (!confirm)
        {
            Debug.LogError("[DeleteSelectUI] DeleteConfirmUI not found on confirmPanel.");
            return;
        }

        Debug.Log($"[DeleteSelectUI] Opening confirm panel for slot {slot}");
        gameObject.SetActive(false);
        confirm.Show(slot, this);
    }

    void OnClickBack()
    {
        if (slotSelectPanel) slotSelectPanel.SetActive(true);
        else Debug.LogWarning("[DeleteSelectUI] slotSelectPanel not assigned.");
        gameObject.SetActive(false);
    }
    void OnEnable()
    {
        QueueFocusFirstButton();
    }

    void QueueFocusFirstButton()
    {
        if (!gameObject.activeInHierarchy) return;
        StopCoroutine(nameof(FocusFirstButtonNextFrame));
        StartCoroutine(FocusFirstButtonNextFrame());
    }

    System.Collections.IEnumerator FocusFirstButtonNextFrame()
    {
        yield return null;

        Button target = null;
        if (firstFocus && firstFocus.gameObject.activeInHierarchy && firstFocus.interactable)
            target = firstFocus;
        else if (btnSlot1 && btnSlot1.gameObject.activeInHierarchy && btnSlot1.interactable)
            target = btnSlot1;
        else if (btnSlot2 && btnSlot2.gameObject.activeInHierarchy && btnSlot2.interactable)
            target = btnSlot2;
        else if (btnSlot3 && btnSlot3.gameObject.activeInHierarchy && btnSlot3.interactable)
            target = btnSlot3;

        if (target)
        {
            var es = EventSystem.current;
            if (es != null)
            {
                es.firstSelectedGameObject = target.gameObject;
                es.SetSelectedGameObject(target.gameObject);
            }
            target.Select();
            Debug.Log($"[DeleteSelectUI] Defaulted focus to {target.name}");
        }
        else
        {
            Debug.LogWarning("[DeleteSelectUI] No button available to select by default.");
        }
    }
}