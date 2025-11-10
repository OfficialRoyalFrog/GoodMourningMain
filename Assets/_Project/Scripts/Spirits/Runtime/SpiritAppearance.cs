using UnityEngine;

/// <summary>
/// Minimal binder that configures a spirit's visuals/collider from its SpiritSO
/// when it spawns. Keeps you to a single base prefab for all spirits.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpiritAppearance : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider fallbackCollider3D;  // if using 3D
    [SerializeField] private SphereCollider autoSphere3D;  // optional cache
    [SerializeField] private float defaultRadius = 0.35f;

    [Header("Optional Tints")]
    [SerializeField] private bool tintByRarity = false;
    [SerializeField] private Color common  = new(1f,1f,1f,1f);
    [SerializeField] private Color uncommon= new(0.85f,1f,0.85f,1f);
    [SerializeField] private Color rare    = new(0.85f,0.9f,1f,1f);
    [SerializeField] private Color epic    = new(1f,0.9f,1f,1f);

    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!fallbackCollider3D) fallbackCollider3D = GetComponent<Collider>();
        if (!autoSphere3D) autoSphere3D = GetComponent<SphereCollider>();
    }

    /// <summary>Call this once right after instantiation to apply visuals per SO.</summary>
    public void Apply(SpiritSO so)
    {
        if (!so) return;
        if (spriteRenderer)
        {
            // Use Portrait as the in-world sprite until/unless you add a dedicated hub sprite to SpiritSO
            if (so.Portrait) spriteRenderer.sprite = so.Portrait;

            if (tintByRarity)
            {
                spriteRenderer.color = so.Rarity switch
                {
                    SpiritRarity.Uncommon => uncommon,
                    SpiritRarity.Rare     => rare,
                    SpiritRarity.Epic     => epic,
                    _                     => common
                };
            }
        }

        // Ensure we always have an easy to hit collider for interactor scans
        if (!fallbackCollider3D)
        {
            autoSphere3D = gameObject.AddComponent<SphereCollider>();
            autoSphere3D.isTrigger = true;
            autoSphere3D.radius = Mathf.Max(0.05f, defaultRadius);
            fallbackCollider3D = autoSphere3D;
        }
        else if (autoSphere3D)
        {
            autoSphere3D.radius = Mathf.Max(0.05f, defaultRadius);
        }

        // Name the instance for easier debugging
        gameObject.name = $"Spirit_{so.DisplayName}";
    }
}