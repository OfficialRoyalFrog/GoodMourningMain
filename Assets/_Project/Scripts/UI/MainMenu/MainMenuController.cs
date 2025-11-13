using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string hubSceneName = "Hub_Base";

    [Header("Panels")]
    [Tooltip("Your first screen (Play/Settings/Achievements/Quit). In your hierarchy this is the column with the four buttons.")]
    [SerializeField] private GameObject rootMenuPanel;   // e.g., LeftColumn
    [SerializeField] private GameObject slotSelectPanel; // will be Panel_SlotSelect (we'll make it)

    // Called from the Play button's OnClick
    public void OnPlayClicked()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager.Instance is null. Ensure a SaveSystem with SaveManager exists in this scene.");
            return;
        }

        bool anySave = SaveManager.Instance.HasSave(1) ||
                       SaveManager.Instance.HasSave(2) ||
                       SaveManager.Instance.HasSave(3);

        if (!anySave)
        {
            // No saves → go straight to game (New Game flow will be refined later)
            SceneManager.LoadScene(hubSceneName, LoadSceneMode.Single);
            return;
        }

        // There is at least one save → show slot select
        ShowSlotSelect();
    }

    public void ShowSlotSelect()
    {
        if (rootMenuPanel) rootMenuPanel.SetActive(false);
        if (slotSelectPanel) slotSelectPanel.SetActive(true);

        var ui = slotSelectPanel ? slotSelectPanel.GetComponent<SlotSelectUI>() : null;
        if (ui) ui.Refresh();
    }

    public void HideSlotSelect()
    {
        if (slotSelectPanel) slotSelectPanel.SetActive(false);
        if (rootMenuPanel) rootMenuPanel.SetActive(true);
    }

    // Optional: hook Quit button
    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}