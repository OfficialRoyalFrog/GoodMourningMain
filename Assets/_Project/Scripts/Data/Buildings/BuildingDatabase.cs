using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Building Database", fileName = "BuildingDatabase")]
public class BuildingDatabase : ScriptableObject
{
    [SerializeField] private List<BuildingSO> buildings = new();
    private Dictionary<string, BuildingSO> map;

    public IReadOnlyList<BuildingSO> AllBuildings => buildings;

    public void RebuildMap()
    {
        map = new Dictionary<string, BuildingSO>();
        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (b == null || string.IsNullOrEmpty(b.Id))
                continue;
            if (map.ContainsKey(b.Id) && map[b.Id] != b)
                Debug.LogWarning($"[BuildingDatabase] Duplicate id '{b.Id}' detected. Overwriting previous entry.");
            map[b.Id] = b;
        }
    }

    public BuildingSO Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var m = Map;
        return m.TryGetValue(id, out var so) ? so : null;
    }

    public Dictionary<string, BuildingSO> Map
    {
        get
        {
            if (map == null)
                RebuildMap();
            return map;
        }
    }

    public List<BuildingSO> Buildings => buildings; // editor use only

    void OnEnable() => map = null;

#if UNITY_EDITOR
    void OnValidate() => map = null;
#endif
}
