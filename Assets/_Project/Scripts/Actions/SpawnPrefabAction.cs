using UnityEngine;

[AddComponentMenu("Interact/Actions/Spawn Prefab")]
public class SpawnPrefabAction : MonoBehaviour, IInteractAction
{
    public GameObject prefab;
    public Transform spawnPoint;
    public bool parentToSpawnPoint = false;

    public bool Execute(PlayerInteractor interactor, InteractableBase owner)
    {
        if (!prefab) return true;
        Transform t = spawnPoint ? spawnPoint : owner.transform;
        var go = Object.Instantiate(prefab, t.position, t.rotation);
        if (parentToSpawnPoint && spawnPoint) go.transform.SetParent(spawnPoint, true);
        return true;
    }
}