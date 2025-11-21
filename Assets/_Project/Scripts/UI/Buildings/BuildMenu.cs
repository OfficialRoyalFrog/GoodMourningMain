using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple UI list that displays unlocked buildings and notifies when the player selects one.
/// Hook this up to a panel under HUD Canvas.
/// </summary>
public class BuildMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject menuRoot;            // panel root to toggle on/off
    [SerializeField] private Transform entryParent;          // content container (e.g., VerticalLayoutGroup)
    [SerializeField] private BuildMenuEntry entryPrefab;     // prefab with icon/name/cost labels
    [SerializeField] private BuildingUnlockService unlockService;
    [SerializeField] private Inventory inventory;

    [Header("State")]
    [SerializeField] private bool openOnStart = false;

    readonly List<BuildMenuEntry> _spawned = new List<BuildMenuEntry>();
    readonly List<BuildingSO> _buildableBuffer = new List<BuildingSO>();

    public event Action<BuildingSO> OnBuildingSelected;
    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (!menuRoot && entryParent)
            menuRoot = entryParent.gameObject;

        if (!inventory)
        {
            inventory = Inventory.Instance;
            if (!inventory)
#if UNITY_2023_1_OR_NEWER
                inventory = FindFirstObjectByType<Inventory>();
#else
                inventory = FindObjectOfType<Inventory>();
#endif
        }

        SetOpen(openOnStart);
    }

    public void Toggle()
    {
        SetOpen(!IsOpen);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (menuRoot) menuRoot.SetActive(open);
        if (open)
            Refresh();
    }

    public void Refresh()
    {
        if (unlockService == null)
        {
            Debug.LogWarning("[BuildMenu] No BuildingUnlockService assigned.", this);
            return;
        }
        ClearEntries();
        var buildables = unlockService.GetBuildableBuildings(_buildableBuffer);
        for (int i = 0; i < buildables.Count; i++)
            CreateEntry(buildables[i]);
    }

    void ClearEntries()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i])
                Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();
    }

    void CreateEntry(BuildingSO building)
    {
        if (!entryPrefab || !entryParent || building == null)
            return;

        var entry = Instantiate(entryPrefab, entryParent);
        entry.Bind(building, inventory, this);
        _spawned.Add(entry);
    }

    internal void HandleEntryClicked(BuildingSO building)
    {
        Debug.Log($"[BuildMenu] Selected {building?.DisplayName ?? building?.Id}", this);
        OnBuildingSelected?.Invoke(building);
        // For now just close menu; placement controller will subscribe to the event later.
        SetOpen(false);
    }
}
