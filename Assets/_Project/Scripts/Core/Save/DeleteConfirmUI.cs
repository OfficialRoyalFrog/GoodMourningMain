using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class DeleteConfirmUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button btnBack;
    [SerializeField] private Button btnAccept;

    [Header("Labels (optional)")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI bodyLabel;

    private int targetSlot = 1;
    private DeleteSelectUI parentList; // to return and refresh after deletion

    void Awake()
    {
        if (btnBack)   btnBack.onClick.AddListener(OnBack);
        if (btnAccept) btnAccept.onClick.AddListener(OnAccept);
    }

    /// Show the confirm panel for a specific slot
    public void Show(int slot, DeleteSelectUI parent)
    {
        targetSlot = slot;
        parentList = parent;

        if (titleLabel) titleLabel.text = "Delete Save";
        if (bodyLabel)  bodyLabel.text = "By confirming, your save will be sacrificed and you will no longer be able to access it.";

        gameObject.SetActive(true);

        // Optional: set initial selection for controller/keyboard navigation
        if (btnBack) btnBack.Select();
    }

    public void OnBack()
    {
        gameObject.SetActive(false);
        if (parentList) parentList.gameObject.SetActive(true);
    }

    public void OnAccept()
    {
        try
        {
            string path = SaveSlots.GetPath(targetSlot);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[DeleteConfirmUI] Deleted save slot {targetSlot} → {path}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DeleteConfirmUI] Delete failed: {e.Message}");
        }

        // Return to the list and refresh it
        gameObject.SetActive(false);
        if (parentList)
        {
            parentList.gameObject.SetActive(true);
            parentList.Refresh();
        }
    }
}