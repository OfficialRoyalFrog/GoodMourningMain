using UnityEngine;

public sealed class SaveUIProxy : MonoBehaviour
{
    // Called by the Pause menu Save button
    public void SaveCurrentSlot()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveNow();   // overwrites the current slot/file
            Debug.Log("[Save] Manual save requested.");
        }
        else
        {
            Debug.LogWarning("[Save] No SaveManager in scene!");
        }
    }
}