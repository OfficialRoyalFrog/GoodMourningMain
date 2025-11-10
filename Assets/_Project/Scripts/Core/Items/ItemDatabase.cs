using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/ItemDatabase", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemSO> items = new();
    private Dictionary<string, ItemSO> map;

    // Public accessor expected by UI systems (read-only view)
    public IReadOnlyList<ItemSO> AllItems => items;

    /// <summary>Rebuild the ID → Item map (call after editing list in editor)</summary>
    public void RebuildMap()
    {
        map = new Dictionary<string, ItemSO>();
        foreach (var it in items)
        {
            if (it == null || string.IsNullOrEmpty(it.Id))
                continue;
            // last write wins; warn on duplicates to help authors
            if (map.ContainsKey(it.Id) && map[it.Id] != it)
                Debug.LogWarning($"[ItemDatabase] Duplicate id '{it.Id}' detected. Overwriting previous entry.");
            map[it.Id] = it;
        }
    }

    public Dictionary<string, ItemSO> Map
    {
        get
        {
            if (map == null)
                RebuildMap();
            return map;
        }
    }

    public ItemSO Get(string id) => Map.TryGetValue(id, out var item) ? item : null;

    private void OnEnable()
    {
        map = null; // force rebuild when asset loads
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        map = null; // force rebuild whenever list changes in editor
    }
#endif

    // Direct inspector management; runtime code should prefer AllItems/Map
    public List<ItemSO> Items => items;
}