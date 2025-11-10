using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class SlotSelectUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button btnSlot1;
    [SerializeField] private Button btnSlot2;
    [SerializeField] private Button btnSlot3;

    [Header("Labels (TMP inside each button)")]
    [SerializeField] private TextMeshProUGUI lblSlot1;
    [SerializeField] private TextMeshProUGUI lblSlot2;
    [SerializeField] private TextMeshProUGUI lblSlot3;

    [Header("Actions")]
    [SerializeField] private Button btnBack;
    [SerializeField] private Button btnDelete; // enabled only when selected slot is occupied
    [SerializeField] private TextMeshProUGUI selectedLabel; // optional; can be null

    [Header("Panels")]
    [SerializeField] private GameObject rootMenuPanel; // assign your LeftColumn/root menu panel here
    [SerializeField] private GameObject deleteSelectPanel; // Panel_DeleteSelect

    [Header("Focus")]
    [SerializeField] private Button firstFocus; // Optional: set in Inspector (e.g., Btn_Slot1)

    [Header("Scenes")]
    [SerializeField] private string hubSceneName = "Hub_Base";

    private int selectedSlot = 1;
    private bool[] occupied = new bool[SaveSlots.MaxSlots + 1]; // 1..3

    void Awake()
    {
        if (btnSlot1) btnSlot1.onClick.AddListener(() => OnClickSlot(1));
        if (btnSlot2) btnSlot2.onClick.AddListener(() => OnClickSlot(2));
        if (btnSlot3) btnSlot3.onClick.AddListener(() => OnClickSlot(3));
        if (btnBack)  btnBack.onClick.AddListener(OnClickBack);
        if (btnDelete) btnDelete.onClick.AddListener(OnClickDelete);
    }

    public void Refresh()
    {
        RefreshSlot(1, lblSlot1);
        RefreshSlot(2, lblSlot2);
        RefreshSlot(3, lblSlot3);
        SelectSlot(1);

        // Queue focus so it happens after the panel is active
        QueueFocusFirstButton();
    }
    void OnEnable()
    {
        // When the panel is shown via SetActive(true), ensure focus is applied next frame
        QueueFocusFirstButton();
    }

    void RefreshSlot(int slot, TextMeshProUGUI label)
    {
        occupied[slot] = SaveSlots.TryReadSummary(slot, out var s);
        if (!label) return;

        if (!occupied[slot]) { label.text = "New Save"; return; }

        string hh = s.hour.ToString("D2");
        string mm = s.minute.ToString("D2");
        label.text = $"Day {s.day}   {hh}:{mm}";
    }

    void SelectSlot(int slot)
    {
        selectedSlot = Mathf.Clamp(slot, 1, SaveSlots.MaxSlots);
        if (selectedLabel) selectedLabel.text = $"Selected: Slot {selectedSlot}";
        if (btnDelete) btnDelete.interactable = occupied[selectedSlot];
    }

    // --- events ---
    void OnClickSlot(int slot)
    {
        SelectSlot(slot);

        if (SaveManager.Instance == null)
        {
            Debug.LogError("[SlotSelectUI] No SaveManager in scene.");
            return;
        }

        // Point the save system at the chosen slot
        SaveManager.Instance.SetCurrentSlot(slot);

        if (occupied[slot])
        {
            // OCCUPIED → Load this slot (scene-aware)
            SaveManager.Instance.LoadNow(slot);
            return;
        }

        // EMPTY → New Game flow:
        // 1) Load Hub scene
        // 2) When loaded, immediately create the initial save for this slot
        SceneManager.sceneLoaded += OnHubLoadedCreateInitialSave;
        SceneManager.LoadScene(hubSceneName);

        void OnHubLoadedCreateInitialSave(Scene _, LoadSceneMode __)
        {
            SceneManager.sceneLoaded -= OnHubLoadedCreateInitialSave;
            SaveManager.Instance.SaveNow();
        }
    }

    void OnClickBack()
    {
        // Return to the root menu without requiring a hard reference to MainMenuController
        if (rootMenuPanel) rootMenuPanel.SetActive(true);
        // Hide this slot select panel (the object this script is attached to)
        gameObject.SetActive(false);
    }

    public void OnClickDelete()
    {
        if (!deleteSelectPanel)
        {
            Debug.LogError("[SlotSelectUI] deleteSelectPanel not assigned!");
            return;
        }

        // Show Delete Select (third column)
        var delUI = deleteSelectPanel.GetComponent<DeleteSelectUI>();
        if (delUI != null)
        {
            delUI.Show();
            deleteSelectPanel.SetActive(true);
            gameObject.SetActive(false); // hide SlotSelect panel
        }
        else
        {
            Debug.LogError("[SlotSelectUI] DeleteSelectUI component missing on deleteSelectPanel.");
        }
    }

    // Expose for next step
    public int GetSelectedSlot() => selectedSlot;
    public bool IsSelectedOccupied() => occupied[selectedSlot];

    void QueueFocusFirstButton()
    {
        if (!gameObject.activeInHierarchy) return;
        StopCoroutine(nameof(FocusFirstButtonNextFrame));
        StartCoroutine(FocusFirstButtonNextFrame());
    }

    System.Collections.IEnumerator FocusFirstButtonNextFrame()
    {
        // Wait one frame so the panel is active and layout is valid
        yield return null;

        // Determine target: prefer inspector-set firstFocus, then Slot1, then 2, then 3
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
            // Set EventSystem selection and Button selection
            var es = EventSystem.current;
            if (es != null)
            {
                es.firstSelectedGameObject = target.gameObject; // helps some UI setups
                es.SetSelectedGameObject(target.gameObject);
            }
            target.Select();
            Debug.Log($"[SlotSelectUI] Defaulted focus to {target.name}");
        }
        else
        {
            Debug.LogWarning("[SlotSelectUI] No slot button available to select by default.");
        }
    }
}