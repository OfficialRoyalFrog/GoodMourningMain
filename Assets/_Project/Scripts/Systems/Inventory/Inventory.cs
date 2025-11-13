using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }
    public event Action OnChanged;

    [Serializable] public class Stack { public ItemSO item; public int count; }

    [Header("Config")]
    [SerializeField] private int maxSlots = 24;

    [Header("Runtime (Readonly)")]
    [SerializeField] private List<Stack> stacks = new(); // stackable items only

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int CountOf(ItemSO item)
    {
        int total = 0;
        for (int i = 0; i < stacks.Count; i++)
            if (stacks[i].item == item) total += stacks[i].count;
        return total;
    }

    public void Add(ItemSO item, int amount)
    {
        if (item == null || amount <= 0) return;
        int remaining = amount;

        // fill existing stacks first
        for (int i = 0; i < stacks.Count && remaining > 0; i++)
        {
            var s = stacks[i];
            if (s.item != item) continue;
            int room = item.MaxStack - s.count;
            if (room <= 0) continue;
            int toAdd = Mathf.Min(room, remaining);
            s.count += toAdd;
            remaining -= toAdd;
        }

        // create new stacks if needed
        while (remaining > 0 && stacks.Count < maxSlots)
        {
            int toAdd = Mathf.Min(item.MaxStack, remaining);
            stacks.Add(new Stack { item = item, count = toAdd });
            remaining -= toAdd;
        }

        if (remaining > 0)
            Debug.LogWarning($"[Inventory] Overflow: could not store {remaining} x {item.Id} (no empty slots).");

        OnChanged?.Invoke();
        Debug.Log($"[Inventory] +{amount - remaining} {item?.Id} (requested {amount})");
    }

    public bool TryConsume(ItemSO item, int amount)
    {
        if (item == null || amount <= 0) return false;
        if (CountOf(item) < amount) { Debug.Log($"[Inventory] Consume FAIL {amount} {item.Id}"); return false; }

        int remaining = amount;
        for (int i = 0; i < stacks.Count && remaining > 0; i++)
        {
            var s = stacks[i];
            if (s.item != item) continue;
            int take = Mathf.Min(s.count, remaining);
            s.count -= take;
            remaining -= take;
        }

        // cleanup empty stacks
        stacks.RemoveAll(s => s.count <= 0);
        OnChanged?.Invoke();
        Debug.Log($"[Inventory] Consumed {amount} {item.Id}");
        return true;
    }

    public IReadOnlyList<Stack> Stacks => stacks;

    // --- Save/Load helpers for later milestones ---
    [Serializable] public struct SaveEntry { public string id; public int count; }

    public List<SaveEntry> ToSaveList()
    {
        var list = new List<SaveEntry>();
        foreach (var s in stacks)
            if (s.item != null)
                list.Add(new SaveEntry { id = s.item.Id, count = s.count });
        return list;
    }

    public void FromSaveList(List<SaveEntry> entries, Dictionary<string, ItemSO> db)
    {
        stacks.Clear();
        if (entries == null) { OnChanged?.Invoke(); return; }
        foreach (var e in entries)
        {
            if (!db.TryGetValue(e.id, out var item) || item == null) continue;
            int remaining = e.count;
            while (remaining > 0 && stacks.Count < maxSlots)
            {
                int toAdd = Mathf.Min(item.MaxStack, remaining);
                stacks.Add(new Stack { item = item, count = toAdd });
                remaining -= toAdd;
            }
        }
        OnChanged?.Invoke();
    }
}