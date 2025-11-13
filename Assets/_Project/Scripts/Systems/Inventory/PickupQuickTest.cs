using UnityEngine;

public class PickupQuickTest : MonoBehaviour
{
    [Header("Assign assets")]
    [SerializeField] private GameObject pickupPrefab; // assign Pickup.prefab
    [SerializeField] private ItemSO lumber;           // your Lumber item
    [SerializeField] private ItemSO stone;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDistance = 2.0f; // in front of player
    [SerializeField] private int amount = 3;

    Transform FindPlayer()
    {
        var p = GameObject.FindWithTag("Player");
        return p ? p.transform : null;
    }

    Vector3 GetSpawnPos()
    {
        var t = FindPlayer();
        if (t == null) return Vector3.zero;
        return t.position + t.forward.normalized * spawnDistance;
    }

    void Spawn(ItemSO item)
    {
        if (!Application.isPlaying) { Debug.LogWarning("[PickupQuickTest] Enter Play Mode to spawn."); return; }
        if (pickupPrefab == null) { Debug.LogWarning("[PickupQuickTest] pickupPrefab not assigned."); return; }
        if (item == null) { Debug.LogWarning("[PickupQuickTest] Item not assigned."); return; }

        var pos = GetSpawnPos();
        var go = Instantiate(pickupPrefab, pos, Quaternion.identity);
        var p = go.GetComponent<Pickup>();
        p.Init(item, amount);
        Debug.Log($"[PickupQuickTest] Spawned {amount} x {item.Id} at {pos}");
    }

    [ContextMenu("Spawn Lumber Pickup")]
    void SpawnLumber() => Spawn(lumber);

    [ContextMenu("Spawn Stone Pickup")]
    void SpawnStone() => Spawn(stone);
}