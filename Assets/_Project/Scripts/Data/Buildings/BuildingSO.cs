using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Building", fileName = "BuildingSO_")]
public class BuildingSO : ScriptableObject
{
    [System.Serializable]
    public class CostEntry
    {
        public ItemSO item;
        [Min(1)] public int amount = 1;
    }

    [Header("Identity")]
    [SerializeField] private string id;                 // unique string key
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;

    [Header("Prefabs")]
    [Tooltip("Ghost/preview prefab to show while placing.")]
    [SerializeField] private GameObject previewPrefab;
    [Tooltip("Construction site prefab (optional).")]
    [SerializeField] private GameObject constructionPrefab;
    [Tooltip("Final built prefab spawned after construction completes.")]
    [SerializeField] private GameObject builtPrefab;

    [Header("Placement")]
    [Tooltip("Grid footprint in tiles (width x depth).")]
    [SerializeField] private Vector2Int footprint = new Vector2Int(2, 2);
    [Tooltip("If true, placement snaps to grid coordinates.")]
    [SerializeField] private bool snapToGrid = true;
    [Tooltip("Layers considered blocking. Builder should reject placement intersecting these.")]
    [SerializeField] private LayerMask blockedLayers = ~0;

    [Header("Costs")]
    [SerializeField] private List<CostEntry> costs = new();

    [Header("Unlock Requirements")]
    [Tooltip("Minimum cult/follower level (or use another metric later).")]
    [SerializeField] private int minimumFollowerLevel = 0;
    [Tooltip("Building ids that must already be built before this one unlocks.")]
    [SerializeField] private List<string> prerequisiteBuildingIds = new();
    [Tooltip("Story/quest flags that must be set for access.")]
    [SerializeField] private List<string> requiredStoryFlags = new();

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;

    public GameObject PreviewPrefab => previewPrefab;
    public GameObject ConstructionPrefab => constructionPrefab;
    public GameObject BuiltPrefab => builtPrefab;

    public Vector2Int Footprint => footprint;
    public bool SnapToGrid => snapToGrid;
    public LayerMask BlockedLayers => blockedLayers;

    public IReadOnlyList<CostEntry> Costs => costs;
    public int MinimumFollowerLevel => minimumFollowerLevel;
    public IReadOnlyList<string> PrerequisiteBuildingIds => prerequisiteBuildingIds;
    public IReadOnlyList<string> RequiredStoryFlags => requiredStoryFlags;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = name.Replace("BuildingSO_", string.Empty).Trim().ToLowerInvariant();
        if (footprint.x < 1) footprint.x = 1;
        if (footprint.y < 1) footprint.y = 1;

        // Clamp amounts but don't strip entries so designers can fill them in later.
        for (int i = 0; i < costs.Count; i++)
        {
            var entry = costs[i];
            if (entry == null) continue;
            if (entry.amount < 1) entry.amount = 1;
        }
    }
#endif
}
