using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central gatekeeper for which buildings are currently unlocked/available.
/// Load/save the unlocked ids later via SaveManager; for now defaults and manual unlocks.
/// </summary>
public class BuildingUnlockService : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private BuildingDatabase buildingDatabase;

    [Header("Starting Unlocks")]
    [Tooltip("Buildings that should start unlocked when no save data exists.")]
    [SerializeField] private List<BuildingSO> defaultUnlockedBuildings = new List<BuildingSO>();

    [Header("Debug")]
    [SerializeField] private bool logUnlocks = false;

    private readonly HashSet<string> _unlocked = new HashSet<string>();

    /// <summary>Read-only snapshot of unlocked ids.</summary>
    public IReadOnlyCollection<string> UnlockedIds => _unlocked;

    void Awake()
    {
        // In the future we'll load from SaveManager. For now, seed defaults on first run.
        SeedDefaults();
    }

    /// <summary>Call after SaveManager loads to apply persisted unlock ids.</summary>
    public void SetUnlockedFromSave(IEnumerable<string> ids)
    {
        _unlocked.Clear();
        if (ids != null)
        {
            foreach (var id in ids)
                if (!string.IsNullOrEmpty(id))
                    _unlocked.Add(id);
        }
        SeedDefaults();
    }

    void SeedDefaults()
    {
        for (int i = 0; i < defaultUnlockedBuildings.Count; i++)
        {
            var so = defaultUnlockedBuildings[i];
            if (so != null && !string.IsNullOrEmpty(so.Id))
                _unlocked.Add(so.Id);
        }
    }

    public void Unlock(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId)) return;
        if (_unlocked.Add(buildingId) && logUnlocks)
            Debug.Log($"[BuildingUnlocks] Unlocked {buildingId}", this);
    }

    public bool IsUnlocked(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId)) return false;
        return _unlocked.Contains(buildingId);
    }

    /// <summary>
    /// Returns true when the building is visible/selectable in the build menu.
    /// Later this can check follower level, story flags, etc.
    /// </summary>
    public bool CanBuild(BuildingSO building)
    {
        if (building == null) return false;
        if (buildingDatabase == null)
        {
            Debug.LogWarning("[BuildingUnlocks] BuildingDatabase missing.", this);
            return false;
        }

        if (!IsUnlocked(building.Id))
            return false;

        if (!MeetsFollowerRequirement(building))
            return false;

        if (!MeetsPrerequisites(building))
            return false;

        if (!MeetsStoryFlags(building))
            return false;

        return true;
    }

    bool MeetsFollowerRequirement(BuildingSO building)
    {
        // TODO: hook into cult/follower level system.
        // For now always true.
        return true;
    }

    bool MeetsPrerequisites(BuildingSO building)
    {
        var prereqs = building.PrerequisiteBuildingIds;
        if (prereqs == null || prereqs.Count == 0)
            return true;

        // TODO: query BuildingManager to see which ids have been built.
        // For now always true.
        return true;
    }

    bool MeetsStoryFlags(BuildingSO building)
    {
        var flags = building.RequiredStoryFlags;
        if (flags == null || flags.Count == 0)
            return true;

        // TODO: check Story/Quest flag system once it exists.
        return true;
    }

    /// <summary>Helper to return all buildings that pass CanBuild() right now.</summary>
    public List<BuildingSO> GetBuildableBuildings(List<BuildingSO> buffer = null)
    {
        buffer ??= new List<BuildingSO>();
        buffer.Clear();
        if (buildingDatabase == null) return buffer;

        var all = buildingDatabase.AllBuildings;
        for (int i = 0; i < all.Count; i++)
        {
            var building = all[i];
            if (building != null && CanBuild(building))
                buffer.Add(building);
        }
        return buffer;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Keep defaults trimmed and unique for convenience.
        // No automatic pruning here; keep authoring-friendly behavior.
    }
#endif
}
