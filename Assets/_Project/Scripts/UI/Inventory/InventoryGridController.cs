using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(90)] // runs before nav builder (100)
public class InventoryGridController : MonoBehaviour
{
    [Header("Data Sources")]
    [SerializeField] Inventory inventory;           // assigned in inspector or auto-found
    [SerializeField] ItemDatabase itemDatabase;     // scriptable object listing all items

    [Header("Grid Sections")]
    [SerializeField] Transform currencyGrid;        // Section_Currency/GridRoot
    [SerializeField] Transform foodGrid;            // Section_Food/GridRoot
    [SerializeField] Transform itemsGrid;           // Section_Items/GridRoot

    [Header("Prefabs")]
    [SerializeField] GameObject slotPrefab;         // InvSlot_Item prefab

    [Header("Display")]
    [SerializeField] bool showEmptyItems = false;   // OFF = hide 0-qty items

    [Header("Nav (optional)")]
    [SerializeField] InventoryLeftNavBuilder nav;   // assign if you want nav rebuilt on visibility changes

    [Header("Empty State (per section)")]
    [SerializeField] GameObject emptyStateCurrency; // assign Section_Currency/EmptyState_Currency
    [SerializeField] GameObject emptyStateFood;     // assign Section_Food/EmptyState_Food
    [SerializeField] GameObject emptyStateItems;    // assign Section_Items/EmptyState_Items

    // runtime caches
    readonly Dictionary<ItemSO, SlotRefs> slotByItem = new();
    // reverse lookup maps for UI helpers
    readonly Dictionary<GameObject, ItemSO> itemByGO = new(); // slot root or selectable -> item

    void Awake()
    {
        // Assign via inspector when possible; fallback to scene lookup for safety
#if UNITY_2023_1_OR_NEWER
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
            if (inventory == null)
                Debug.LogError("[InventoryGridController] No Inventory found in scene. Assign it in the inspector or add one to the scene.");
        }

        if (itemDatabase == null)
        {
            itemDatabase = FindFirstObjectByType<ItemDatabase>(FindObjectsInactive.Include);
            if (itemDatabase == null)
                Debug.LogError("[InventoryGridController] No ItemDatabase found in scene. Assign it in the inspector or add one to the scene.");
        }
#else
        if (inventory == null)
        {
            inventory = FindObjectOfType<Inventory>();
            if (inventory == null)
                Debug.LogError("[InventoryGridController] No Inventory found in scene. Assign it in the inspector or add one to the scene.");
        }

        if (itemDatabase == null)
        {
            itemDatabase = FindObjectOfType<ItemDatabase>();
            if (itemDatabase == null)
                Debug.LogError("[InventoryGridController] No ItemDatabase found in scene. Assign it in the inspector or add one to the scene.");
        }
#endif
    }

    void OnEnable()
    {
        BuildIfNeeded();

        if (inventory != null)
            inventory.OnChanged += RefreshAll;

        RefreshAll();
    }

    void OnDisable()
    {
        if (inventory != null)
            inventory.OnChanged -= RefreshAll;
    }

    /// <summary>
    /// Builds slot grids once (one slot per known item).
    /// </summary>
    public void BuildIfNeeded()
    {
        if (slotByItem.Count > 0) return; // already built

        Clear(currencyGrid);
        Clear(foodGrid);
        Clear(itemsGrid);

        if (itemDatabase == null || itemDatabase.AllItems == null || itemDatabase.AllItems.Count == 0)
        {
            Debug.LogWarning("[InventoryGridController] No items found. Ensure ItemDatabase is assigned and has items.");
            return;
        }

        // Keep deterministic ordering by display name, fallback to asset name
        var sourceItems = new List<ItemSO>(itemDatabase.AllItems);
        sourceItems.Sort((a, b) => string.Compare(a?.DisplayName ?? a?.name, b?.DisplayName ?? b?.name, System.StringComparison.Ordinal));

        var seen = new HashSet<ItemSO>();
        foreach (var item in sourceItems)
        {
            if (item == null || !seen.Add(item)) continue;

            Transform parent = itemsGrid; // default bucket
            switch (item.Category)
            {
                case ItemCategory.Currency: parent = currencyGrid; break;
                case ItemCategory.Food:     parent = foodGrid;     break;
                // ItemCategory.Item and any future categories fall back to itemsGrid
            }

            if (!parent)
            {
                Debug.LogWarning($"[InventoryGridController] Missing grid parent for category {item.Category}. Item: {item.name}");
                parent = itemsGrid;
            }

            var go = Instantiate(slotPrefab, parent);
            // map both root and nested selectable to item for selection/autofocus helpers
            var selectable = go.GetComponent<Selectable>() ?? go.GetComponentInChildren<Selectable>(true);

            go.name = $"Slot_{(string.IsNullOrEmpty(item.DisplayName) ? item.name : item.DisplayName)}";
            var refs = WireSlot(go, item);
            slotByItem[item] = refs;

            // reverse maps
            itemByGO[go] = item;
            if (selectable != null)
                itemByGO[selectable.gameObject] = item;

            // initialize visuals (and visibility) at build time
            _ = UpdateVisual(refs, 0);
        }
    }

    void Clear(Transform grid)
    {
        if (!grid) return;
        for (int i = grid.childCount - 1; i >= 0; i--)
            Destroy(grid.GetChild(i).gameObject);
    }

    SlotRefs WireSlot(GameObject go, ItemSO item)
    {
        var refs = new SlotRefs
        {
            item  = item,
            root  = go,
            icon  = go.transform.Find("Icon")?.GetComponent<Image>(),
            count = go.transform.Find("Count")?.GetComponent<TMP_Text>(),
            border= go.transform.Find("Border")?.GetComponent<Image>()
        };

        if (refs.icon)
            refs.icon.sprite = item.Icon;

        return refs;
    }

    /// <summary>Refresh all visible counts from Inventory</summary>
    public void RefreshAll()
    {
        if (inventory == null) return;

        bool anyVisibilityChange = false;

        foreach (var kv in slotByItem)
        {
            int amount = inventory.CountOf(kv.Key);
            bool changed = UpdateVisual(kv.Value, amount);
            anyVisibilityChange |= changed;
        }

        // --- Per-section empty-state toggles ---
        bool curHas  = HasVisibleSlots(currencyGrid);
        bool foodHas = HasVisibleSlots(foodGrid);
        bool itemHas = HasVisibleSlots(itemsGrid);

        bool emptyChanged = false;
        emptyChanged |= SetActiveIfChanged(emptyStateCurrency, !curHas);
        emptyChanged |= SetActiveIfChanged(emptyStateFood,     !foodHas);
        emptyChanged |= SetActiveIfChanged(emptyStateItems,    !itemHas);

        // Rebuild controller/D-pad nav if visible set or empty-state changed
        if ((anyVisibilityChange || emptyChanged) && nav != null)
        {
            nav.Rebuild();
        }
    }

    /// <summary>
    /// Update visuals for a slot. Returns true if the slot's active state changed.
    /// </summary>
    bool UpdateVisual(SlotRefs refs, int amount)
    {
        if (refs.count)
            refs.count.text = amount.ToString();

        if (refs.icon)
            refs.icon.color = amount > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f);

        // Show/hide based on amount (unless showing empties)
        bool visible = showEmptyItems || amount > 0;
        bool changed = refs.root.activeSelf != visible;
        if (changed)
            refs.root.SetActive(visible);

        // (Optional place to tint BG/border by rarity, etc.)
        return changed;
    }

    /// <summary>
    /// Returns the instantiated slot GameObject for a given item, or null if not found.
    /// </summary>
    public GameObject GetSlotForItem(ItemSO item)
    {
        if (item == null) return null;
        if (slotByItem.TryGetValue(item, out var refs) && refs != null)
            return refs.root;
        return null;
    }

    /// <summary>Returns the ItemSO bound to a specific slot GameObject, or null.</summary>
    public ItemSO GetItemForSlotGO(GameObject slotGO)
    {
        if (!slotGO) return null;
        if (itemByGO.TryGetValue(slotGO, out var item)) return item;
        return null;
    }

    /// <summary>
    /// Scans a section root and returns the first slot GameObject whose item count > 0.
    /// Returns the actual Selectable's GameObject so EventSystem can select it.
    /// </summary>
    public GameObject GetFirstOwnedInSection(Transform sectionRoot, Inventory inv)
    {
        if (!sectionRoot || inv == null) return null;
        for (int i = 0; i < sectionRoot.childCount; i++)
        {
            var child = sectionRoot.GetChild(i);
            if (!child.gameObject.activeInHierarchy) continue;

            var sel = GetSelectableOnOrUnder(child);
            if (sel == null || !sel.interactable) continue;

            // Try to resolve the item from selectable GO first, then the child root
            if (!itemByGO.TryGetValue(sel.gameObject, out var item) || item == null)
                itemByGO.TryGetValue(child.gameObject, out item);
            if (item == null) continue;

            if (inv.CountOf(item) > 0)
                return sel.gameObject; // return the actual selectable
        }
        return null;
    }

    private static Selectable GetSelectableOnOrUnder(Transform t)
    {
        if (!t) return null;
        if (t.TryGetComponent<Selectable>(out var selHere)) return selHere;
        return t.GetComponentInChildren<Selectable>(true);
    }

    // True if the grid has at least one visible (active) SLOT child
    bool HasVisibleSlots(Transform gridRoot)
    {
        if (!gridRoot) return false;

        for (int i = 0; i < gridRoot.childCount; i++)
        {
            var child = gridRoot.GetChild(i);
            if (!child.gameObject.activeInHierarchy) continue;

            // Count only real slot entries (defensive):
            // 1) Anything with a Selectable (your slot/select button), or
            // 2) Named like your slots (Slot_*), so UI panels like EmptyState_* are ignored.
            if (child.GetComponent<UnityEngine.UI.Selectable>() != null) return true;
            if (child.name.StartsWith("Slot_")) return true;
        }
        return false;
    }

    // SetActive only if state changes; return whether it changed
    bool SetActiveIfChanged(GameObject go, bool active)
    {
        if (!go) return false;
        if (go.activeSelf == active) return false;
        go.SetActive(active);
        return true;
    }

    class SlotRefs
    {
        public ItemSO item;
        public GameObject root;
        public Image icon;
        public TMP_Text count;
        public Image border;
    }
}