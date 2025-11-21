using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Core runtime state for world building placement.
/// This component listens for BuildMenu selections, spawns the preview object,
/// and later (future steps) will handle validation/placement.
/// </summary>
[DisallowMultipleComponent]
public sealed class BuildingPlacementController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Build menu that raises OnBuildingSelected events.")]
    [SerializeField] private BuildMenu buildMenu;
    [Tooltip("Inventory used to check/consume costs when confirming placement.")]
    [SerializeField] private Inventory inventory;
    [Tooltip("Camera used for placement raycasts (defaults to Camera.main).")]
    [SerializeField] private Camera placementCamera;

    [Header("Placement Settings")]
    [Tooltip("Maximum distance from the camera ray hit to consider for placement.")]
    [SerializeField, Min(1f)] private float maxPlacementDistance = 25f;
    [Tooltip("Layers that the placement ray is allowed to collide with for positioning.")]
    [SerializeField] private LayerMask groundMask = -1;
    [Tooltip("If true, placements are rejected when colliders on these layers overlap the footprint.")]
    [SerializeField] private bool useBlockingLayers = true;
    [SerializeField] private LayerMask blockingLayers = ~0;
    [SerializeField, Min(0.1f)] private float blockingHeight = 1f;
    [SerializeField, Min(0.25f)] private float gridSize = 1f;
    [SerializeField, Min(0f)] private float placementHeightOffset = 0f;

    [Header("Preview Visuals")]
    [Tooltip("Optional override material when placement is valid.")]
    [SerializeField] private Material previewValidMaterial;
    [Tooltip("Optional override material when placement is invalid.")]
    [SerializeField] private Material previewInvalidMaterial;

    // ----- runtime state -----
    BuildingSO _activeBuilding;
    GameObject _previewInstance;
    Renderer[] _previewRenderers = System.Array.Empty<Renderer>();
    Material[][] _previewOriginalMaterials;
    Vector3 _currentPlacementPosition;
    bool _placementValid;
    bool _subscribedToMenu;
    Collider _lastGroundCollider;
    readonly Collider[] _overlapBuffer = new Collider[32];

    void OnEnable()
    {
        if (!placementCamera)
            placementCamera = Camera.main;

        ResolveDependencies();

        TrySubscribeToMenu();
    }

    void OnDisable()
    {
        if (buildMenu && _subscribedToMenu)
        {
            buildMenu.OnBuildingSelected -= BeginPlacement;
            _subscribedToMenu = false;
        }

        CancelPlacement();
    }

    void OnValidate()
    {
        if (!placementCamera)
            placementCamera = Camera.main;
    }

    void ResolveDependencies()
    {
        if (!buildMenu)
            buildMenu = FindBuildMenuIncludingInactive();

        TrySubscribeToMenu();

        if (!inventory)
        {
            inventory = Inventory.Instance;
            if (!inventory)
#if UNITY_2023_1_OR_NEWER
                inventory = FindFirstObjectByType<Inventory>();
#else
                inventory = FindObjectOfType<Inventory>();
#endif
        }
    }

    void TrySubscribeToMenu()
    {
        if (buildMenu == null || _subscribedToMenu)
            return;
        buildMenu.OnBuildingSelected += BeginPlacement;
        _subscribedToMenu = true;
    }

    BuildMenu FindBuildMenuIncludingInactive()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<BuildMenu>(FindObjectsInactive.Include);
#else
        var menus = Resources.FindObjectsOfTypeAll<BuildMenu>();
        return (menus != null && menus.Length > 0) ? menus[0] : null;
#endif
    }

    void Update()
    {
        if (!buildMenu || !inventory)
            ResolveDependencies();

        if (!_activeBuilding)
            return;

        if (!_previewInstance)
            SpawnPreviewInstance();

        UpdatePreviewTransform();
        HandlePlacementInput();
    }

    /// <summary>
    /// Called when the player picks a building from the BuildMenu.
    /// </summary>
    public void BeginPlacement(BuildingSO building)
    {
        if (!building)
            return;

        ResolveDependencies();

        Debug.Log($"[Placement] BeginPlacement -> {building.DisplayName}");
        CancelPlacement();
        _activeBuilding = building;
        SpawnPreviewInstance();
    }

    /// <summary>
    /// Cancels any pending placement and destroys the preview instance.
    /// </summary>
    public void CancelPlacement()
    {
        _activeBuilding = null;
        _placementValid = false;

        if (_previewInstance)
        {
            Destroy(_previewInstance);
            _previewInstance = null;
            _previewRenderers = System.Array.Empty<Renderer>();
            _previewOriginalMaterials = null;
        }

        _lastGroundCollider = null;
    }

    void SpawnPreviewInstance()
    {
        if (!_activeBuilding) return;
        var prefab = ResolvePreviewPrefab(_activeBuilding);
        if (!prefab)
        {
            Debug.LogWarning("[Placement] No preview/built prefab assigned on BuildingSO.", _activeBuilding);
            _activeBuilding = null;
            return;
        }

        _previewInstance = Instantiate(prefab);
        _previewInstance.name = prefab.name + "_Preview";

        foreach (var col in _previewInstance.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        _previewRenderers = _previewInstance.GetComponentsInChildren<Renderer>(true);
        _previewOriginalMaterials = new Material[_previewRenderers.Length][];
        for (int i = 0; i < _previewRenderers.Length; i++)
            _previewOriginalMaterials[i] = _previewRenderers[i].materials;

        ApplyPreviewMaterial(previewInvalidMaterial);
    }

    GameObject ResolvePreviewPrefab(BuildingSO building)
    {
        if (building.PreviewPrefab) return building.PreviewPrefab;
        if (building.ConstructionPrefab) return building.ConstructionPrefab;
        return building.BuiltPrefab;
    }

    void UpdatePreviewTransform()
    {
        if (!_previewInstance || !_activeBuilding) return;

        bool hasPoint = TryGetPlacementPoint(out var position, out var normal);
        if (!hasPoint)
        {
            _placementValid = false;
            ApplyPreviewMaterial(previewInvalidMaterial);
            return;
        }

        _currentPlacementPosition = position;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
        _previewInstance.transform.SetPositionAndRotation(position, rotation);

        _placementValid = ValidatePlacement(position);
        var mat = _placementValid ? previewValidMaterial : previewInvalidMaterial;
        ApplyPreviewMaterial(mat);
    }

    bool TryGetPlacementPoint(out Vector3 position, out Vector3 normal)
    {
        position = Vector3.zero;
        normal = Vector3.up;
        if (!placementCamera) return false;
        if (Mouse.current == null) return false;

        var pointerPos = Mouse.current.position.ReadValue();
        var ray = placementCamera.ScreenPointToRay(pointerPos);
        if (!Physics.Raycast(ray, out var hit, maxPlacementDistance, groundMask, QueryTriggerInteraction.Ignore))
            return false;

        var snapped = hit.point;
        if (_activeBuilding.SnapToGrid)
        {
            snapped.x = Mathf.Round(snapped.x / gridSize) * gridSize;
            snapped.z = Mathf.Round(snapped.z / gridSize) * gridSize;
        }
        snapped.y = hit.point.y + placementHeightOffset;

        position = snapped;
        normal = hit.normal;
        _lastGroundCollider = hit.collider;
        return true;
    }

    bool ValidatePlacement(Vector3 center)
    {
        if (!_activeBuilding) return false;

        var footprint = _activeBuilding.Footprint;
        float width  = Mathf.Max(gridSize, footprint.x * gridSize);
        float depth  = Mathf.Max(gridSize, footprint.y * gridSize);
        float height = Mathf.Max(0.1f, blockingHeight);
        Vector3 halfExtents = new Vector3(width * 0.5f, height * 0.5f, depth * 0.5f);

        if (!useBlockingLayers)
            return true;

        int layers = blockingLayers.value;
        int buildingLayerMask = _activeBuilding.BlockedLayers.value;
        if (buildingLayerMask != 0)
            layers = buildingLayerMask;

        if (layers == 0)
            return true;

        Vector3 checkCenter = center + Vector3.up * (halfExtents.y + placementHeightOffset + 0.05f);
        int hitCount = Physics.OverlapBoxNonAlloc(
            checkCenter,
            halfExtents,
            _overlapBuffer,
            Quaternion.identity,
            layers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            var col = _overlapBuffer[i];
            _overlapBuffer[i] = null;
            if (!col || col == _lastGroundCollider)
                continue;

            Debug.Log($"[Placement] Blocked by {col.name} (layer {LayerMask.LayerToName(col.gameObject.layer)})", this);
            return false;
        }

        return true;
    }

    bool HasResources(BuildingSO building)
    {
        if (building == null) return false;

        var costs = building.Costs;
        if (costs == null || costs.Count == 0)
            return true;

        if (!inventory)
            return false;

        foreach (var cost in costs)
        {
            if (cost == null || cost.item == null) continue;
            if (inventory.CountOf(cost.item) < cost.amount)
                return false;
        }
        return true;
    }

    void HandlePlacementInput()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryConfirmPlacement();
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame ||
                 (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            CancelPlacement();
        }
    }

    void TryConfirmPlacement()
    {
        if (_activeBuilding == null)
        {
            Debug.LogWarning("[Placement] TryConfirmPlacement with no active building.", this);
            return;
        }

        if (!_placementValid)
        {
            Debug.Log("[Placement] Placement invalid at current position.", this);
            return;
        }

        if (!HasResources(_activeBuilding))
        {
            Debug.Log("[Placement] Not enough resources to confirm placement.", this);
            return;
        }

        if (!SpendResources(_activeBuilding))
        {
            Debug.Log("[Placement] SpendResources failed.", this);
            return;
        }

        var prefab = _activeBuilding.BuiltPrefab ?? _activeBuilding.ConstructionPrefab;
        if (!prefab)
        {
            Debug.LogWarning("[Placement] No built prefab assigned to BuildingSO.", _activeBuilding);
            CancelPlacement();
            return;
        }

        Instantiate(prefab, _currentPlacementPosition, _previewInstance ? _previewInstance.transform.rotation : Quaternion.identity);
        Debug.Log($"[Placement] Spawned {prefab.name} at {_currentPlacementPosition}", this);
        CancelPlacement();
    }

    bool SpendResources(BuildingSO building)
    {
        if (building == null) return false;

        var costs = building.Costs;
        if (costs == null || costs.Count == 0)
            return true;

        if (!inventory)
            return false;

        if (!HasResources(building))
            return false;

        foreach (var cost in costs)
        {
            if (cost == null || cost.item == null) continue;
            if (!inventory.TryConsume(cost.item, cost.amount))
            {
                Debug.LogWarning($"[Placement] Failed to consume {cost.amount}x {cost.item.DisplayName}", this);
                return false;
            }
        }
        return true;
    }

    void ApplyPreviewMaterial(Material overrideMat)
    {
        if (_previewRenderers == null) return;
        for (int i = 0; i < _previewRenderers.Length; i++)
        {
            var renderer = _previewRenderers[i];
            if (!renderer) continue;

            if (overrideMat == null)
            {
                if (_previewOriginalMaterials != null && i < _previewOriginalMaterials.Length)
                    renderer.materials = _previewOriginalMaterials[i];
            }
            else
            {
                var mats = renderer.materials;
                for (int m = 0; m < mats.Length; m++)
                    mats[m] = overrideMat;
                renderer.materials = mats;
            }
        }
    }
}
