using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Pickup : MonoBehaviour
{
    [Header("Item Payload")]
    [SerializeField] private ItemSO item;
    [SerializeField, Min(1)] private int amount = 1;

    [Header("Auto-Collect & Magnet")]
    [SerializeField] private float autoCollectRadius = 1.8f; // immediate collect
    [SerializeField] private float magnetRadius = 5f;        // begin pulling toward player
    [SerializeField] private float magnetSpeed = 8f;         // units/sec

    [Header("Visual")]
    [SerializeField] private SpriteRenderer iconRenderer;

    private Transform player;
    private bool collected;
    private float enableCollectAt = 0f; // spawn grace period before collecting

    // Call this after Instantiate to configure payload
    public void Init(ItemSO i, int a)
    {
        item = i;
        amount = Mathf.Max(1, a);
        if (iconRenderer != null && i != null) iconRenderer.sprite = i.Icon;
    }

    void Reset()
    {
        // Ensure collider is trigger and small
        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.3f;
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p) player = p.transform;
        else Debug.LogWarning("[Pickup] No GameObject with tag 'Player' found.");

        enableCollectAt = Time.time + 0.25f; // quarter-second grace so pickup is visible
        if (iconRenderer != null && item != null && iconRenderer.sprite == null)
            iconRenderer.sprite = item.Icon;
    }

    void Update()
    {
        if (collected || player == null) return;
        if (Time.time < enableCollectAt) return;

        float d = Vector3.Distance(transform.position, player.position);

        // auto collect when very close
        if (d <= autoCollectRadius)
        {
            Collect();
            return;
        }

        // magnet pull
        if (d <= magnetRadius)
        {
            Vector3 target = player.position + Vector3.up * 0.5f;
            transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || Time.time < enableCollectAt) return;
        if (other.CompareTag("Player")) Collect();
    }

    void Collect()
    {
        if (collected) return;
        collected = true;

        if (item != null && amount > 0 && Inventory.Instance != null)
        {
            Inventory.Instance.Add(item, amount);
            // Toast: show +X and updated total
            if (ToastManager.Instance != null)
            {
                ToastManager.Instance.ShowItemToast(item, amount);
            }
            // SFX/VFX hooks go here (we’ll wire later)
        }

        Destroy(gameObject);
    }
}