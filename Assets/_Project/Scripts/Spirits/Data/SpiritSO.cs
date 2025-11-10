using UnityEngine;

public enum SpiritRarity
{
    Common,
    Uncommon,
    Rare,
    Epic
}

[CreateAssetMenu(menuName = "Game/Spirit", fileName = "SpiritSO_")]
public class SpiritSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id;              // unique, stable (e.g., "spirit_a")
    [SerializeField] private string displayName;     // UI name

    [Header("Presentation")]
    [SerializeField] private Sprite portrait;        // sidebar/collection portrait
    [SerializeField] private GameObject hubPrefab;   // spawned in hub later

    [Header("Optional")]
    [SerializeField] private SpiritRarity rarity = SpiritRarity.Common;
    [TextArea(2, 5)]
    [SerializeField] private string flavor;

    [Header("Starting Profile (optional)")]
    [SerializeField] private bool overrideStartingState = false;
    [SerializeField, Range(0f,1f)] private float startSerenity01 = 0.5f;
    [SerializeField, Range(0f,1f)] private float startAppetite01 = 1.0f;
    [SerializeField, Range(0f,1f)] private float startIntegrity01 = 1.0f; // wellness: full = healthy

    [Header("Per-Spirit Multipliers (optional)")]
    [Tooltip("Multiply global tuning by these per-spirit factors. Leave at 1 for normal.")]
    [SerializeField, Min(0f)] private float serenityRegenMult = 1f;
    [SerializeField, Min(0f)] private float appetiteDecayMult = 1f;
    [SerializeField, Min(0f)] private float integrityRegenKMult = 1f;
    [SerializeField, Min(0f)] private float appetitePenaltyKMult = 1f;

    public bool OverrideStartingState => overrideStartingState;
    public float StartSerenity01 => startSerenity01;
    public float StartAppetite01 => startAppetite01;
    public float StartIntegrity01 => startIntegrity01;

    public float SerenityRegenMult => serenityRegenMult;
    public float AppetiteDecayMult => appetiteDecayMult;
    public float IntegrityRegenKMult => integrityRegenKMult;
    public float AppetitePenaltyKMult => appetitePenaltyKMult;

    public string Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Portrait => portrait;
    public GameObject HubPrefab => hubPrefab;
    public SpiritRarity Rarity => rarity;
    public string Flavor => flavor;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-fill a sane ID if empty: "spiritso_spirita" -> "spirita"
        if (string.IsNullOrWhiteSpace(id))
        {
            var auto = name.Replace("SpiritSO_", string.Empty).Trim();
            id = auto.ToLowerInvariant().Replace(' ', '_');
        }
    }
#endif
}