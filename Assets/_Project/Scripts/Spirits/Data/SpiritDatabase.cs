using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Spirit Database", fileName = "SpiritDatabase")]
public class SpiritDatabase : ScriptableObject
{
    [SerializeField] private List<SpiritSO> spirits = new();
    private Dictionary<string, SpiritSO> map;

    public IReadOnlyList<SpiritSO> AllSpirits => spirits;

    /// <summary>Call to rebuild ID -> SO map (runs automatically on load/validate).</summary>
    public void RebuildMap()
    {
        map = new Dictionary<string, SpiritSO>();
        foreach (var s in spirits)
        {
            if (s == null || string.IsNullOrEmpty(s.Id)) continue;
            if (map.ContainsKey(s.Id) && map[s.Id] != s)
                Debug.LogWarning($"[SpiritDatabase] Duplicate id '{s.Id}' detected. Overwriting previous entry.");
            map[s.Id] = s;
        }
    }

    public SpiritSO Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (map == null) RebuildMap();
        return map.TryGetValue(id, out var so) ? so : null;
    }

    private void OnEnable()  { map = null; } // force rebuild on load
#if UNITY_EDITOR
    private void OnValidate(){ map = null; } // force rebuild when edited
#endif

    // Optional direct access if you want to reorder in inspector
    public List<SpiritSO> Spirits => spirits;
}