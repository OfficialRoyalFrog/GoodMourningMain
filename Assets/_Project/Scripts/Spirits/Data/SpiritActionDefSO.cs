using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Spirits/Action", fileName = "Action_")]
public class SpiritActionDefSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id;                   // stable, e.g., "work_farm_lumber"
    [SerializeField] private string displayName;          // UI label
    [SerializeField] private ActionCategory category = ActionCategory.Work;
    [SerializeField] private Sprite icon;

    [Header("Availability")]
    [Tooltip("If true, hide/disable this action from menus.")]
    [SerializeField] private bool disabled = false;

    [Header("Cooldown")]
    [Tooltip("Hours between uses per spirit (in-game hours). 0 = no cooldown.")]
    [SerializeField, Min(0f)] private float cooldownHours = 0f;

    [Header("Execution")]
    [Tooltip("If > 0, this is a time assignment (job). 0 = instant effect.")]
    [SerializeField, Min(0f)] private float assignmentDurationHours = 0f;

    [Tooltip("Optional station tag required in the world (e.g., \"FarmPlot\", \"LumberYard\"). Leave blank for none.")]
    [SerializeField] private string requiredStationTag;

    [Header("Effects (instant or on-complete)")]
    [Tooltip("Meters change by these amounts (can be negative).")]
    [SerializeField] private float deltaSerenity = 0f;
    [SerializeField] private float deltaAppetite = 0f;
    [SerializeField] private float deltaIntegrity = 0f;
    [SerializeField, Min(0f)] private float xpGain = 0f;

    [Header("Inventory (optional)")]
    [Tooltip("If true, requires consuming an item before executing.")]
    [SerializeField] private bool consumesItem = false;
    [SerializeField] private ItemSO requiredItem;
    [SerializeField, Min(1)] private int requiredItemCount = 1;

    // --- Public accessors ---
    public string Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public ActionCategory Category => category;
    public Sprite Icon => icon;
    public bool Disabled => disabled;
    public float CooldownHours => cooldownHours;
    public float AssignmentDurationHours => assignmentDurationHours;
    public string RequiredStationTag => requiredStationTag;
    public float DeltaSerenity => deltaSerenity;
    public float DeltaAppetite => deltaAppetite;
    public float DeltaIntegrity => deltaIntegrity;
    public float XpGain => xpGain;
    public bool ConsumesItem => consumesItem;
    public ItemSO RequiredItem => requiredItem;
    public int RequiredItemCount => requiredItemCount;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Generate a sane, stable id only if empty
        if (string.IsNullOrWhiteSpace(id))
        {
            var auto = name.Replace("Action_", "").Trim();
            auto = Regex.Replace(auto, "[^a-zA-Z0-9_]+", "_"); // sanitize
            id = auto.ToLowerInvariant().Trim('_');
        }

        // Enforce non-negative numeric fields
        if (cooldownHours < 0f) cooldownHours = 0f;
        if (assignmentDurationHours < 0f) assignmentDurationHours = 0f;

        // Item gating: if we don't consume, clear the item to avoid confusing inspector drawers
        if (!consumesItem)
        {
            requiredItem = null;
            if (requiredItemCount != 1) requiredItemCount = 1; // keep a sane default
        }
        else
        {
            if (requiredItemCount < 1) requiredItemCount = 1;
        }
    }
#endif
}