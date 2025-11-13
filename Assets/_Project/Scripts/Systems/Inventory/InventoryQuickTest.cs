using UnityEngine;

public class InventoryQuickTest : MonoBehaviour
{
    [Header("Assign the ItemSO assets here")]
    [SerializeField] private ItemSO lumber;
    [SerializeField] private ItemSO stone;

    [ContextMenu("Give 5 Lumber")]
    void Give5Lumber()
    {
        if (Inventory.Instance == null) { Debug.LogWarning("[QuickTest] No Inventory.Instance"); return; }
        if (lumber == null) { Debug.LogWarning("[QuickTest] Lumber not assigned"); return; }
        Inventory.Instance.Add(lumber, 5);
        Debug.Log($"[QuickTest] After add: Lumber total = {Inventory.Instance.CountOf(lumber)}");
    }

    [ContextMenu("Give 5 Stone")]
    void Give5Stone()
    {
        if (Inventory.Instance == null) { Debug.LogWarning("[QuickTest] No Inventory.Instance"); return; }
        if (stone == null) { Debug.LogWarning("[QuickTest] Stone not assigned"); return; }
        Inventory.Instance.Add(stone, 5);
        Debug.Log($"[QuickTest] After add: Stone total = {Inventory.Instance.CountOf(stone)}");
    }

    [ContextMenu("Consume All Lumber & Stone")]
    void ConsumeAll()
    {
        if (Inventory.Instance == null) { Debug.LogWarning("[QuickTest] No Inventory.Instance"); return; }

        if (lumber != null)
        {
            int w = Inventory.Instance.CountOf(lumber);
            if (w > 0) Inventory.Instance.TryConsume(lumber, w);
            Debug.Log($"[QuickTest] Consumed Lumber: {w}");
        }

        if (stone != null)
        {
            int s = Inventory.Instance.CountOf(stone);
            if (s > 0) Inventory.Instance.TryConsume(stone, s);
            Debug.Log($"[QuickTest] Consumed Stone: {s}");
        }
    }
}