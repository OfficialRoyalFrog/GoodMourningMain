using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryMenu : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    public static ItemSO LastSelectedItem; // remembered across opens to restore selection

    [Header("UI")]
    [SerializeField] GameObject inventoryPanel;   // HUD_Canvas/InventoryPanel (root)
    [SerializeField] Animator  sidebarAnimator;   // InventoryPanel/Sidebar (Bool "IsOpen")

    [Header("Animation Timing")]
    [Tooltip("Length of slide OUT animation in seconds (must match your OUT transition).")]
    [SerializeField] float slideDuration = 0.20f;

    [Header("Pause Integration")]
    [Tooltip("If true, uses GamePause to pause while the inventory is open.")]
    [SerializeField] bool pauseWhileOpen = true;
    [SerializeField] GamePause gamePause;         // drag your Systems/GamePause here

    [Header("Optional: Prep + Focus")]
    [SerializeField] InventoryGridController grid;      // optional: ensure slots exist
    [SerializeField] InventoryLeftNavBuilder nav;       // optional: rebuild nav

    [Header("Autofocus (inline)")]
    [SerializeField] Inventory inventory;                  // Systems/Inventory
    [SerializeField] Transform sectionCurrencyGrid;       // assign Grid_Currency
    [SerializeField] Transform sectionFoodGrid;           // assign Grid_Food
    [SerializeField] Transform sectionItemsGrid;          // assign Grid_Items

    Coroutine closeRoutine;

    private float _prevTimeScale = 1f;
    private bool _changedTimeScale = false;

    [Header("Input Debounce")]
    [SerializeField] private float toggleDebounceSeconds = 0.15f; // prevents double-fire from multiple maps/press repeats
    private int   _lastToggleFrame = -1;
    private float _nextToggleTime  = 0f;

    private static readonly BindingFlags ItemBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

    /// <summary>Open or close, mirroring GamePause.SetPaused(bool).</summary>
    public void SetOpen(bool open)
    {
        if (open)
        {
            // Already open (and not mid-close)
            if (IsOpen && closeRoutine == null) return;
            OpenInventory();
            return;
        }

        if (!IsOpen && closeRoutine == null) return;
        BeginCloseInventory();
    }

    void OpenInventory()
    {
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        if (grid) grid.BuildIfNeeded();
        if (nav)  nav.Rebuild();

        if (inventoryPanel && !inventoryPanel.activeSelf)
            inventoryPanel.SetActive(true);

        if (sidebarAnimator)
        {
            sidebarAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            sidebarAnimator.SetBool("IsOpen", true);
        }

        if (grid)
            grid.RefreshAll();

        Canvas.ForceUpdateCanvases();

        if (pauseWhileOpen && Time.timeScale != 0f)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _changedTimeScale = true;
        }

        StartCoroutine(FocusFirstOwnedAfterOpen());
        IsOpen = true;
    }

    void BeginCloseInventory()
    {
        CacheLastSelectedItem();

        if (!IsOpen) return;

        if (sidebarAnimator)
            sidebarAnimator.SetBool("IsOpen", false);

        if (closeRoutine != null) StopCoroutine(closeRoutine);
        closeRoutine = StartCoroutine(CloseAfterSlide());
    }

    void CacheLastSelectedItem()
    {
        var selectedGO = EventSystem.current?.currentSelectedGameObject;
        if (!selectedGO) return;

        ItemSO found = null;

        if (grid != null)
        {
            found = grid.GetItemForSlotGO(selectedGO);
            if (found == null)
            {
                var selectable = selectedGO.GetComponent<Selectable>() ?? selectedGO.GetComponentInParent<Selectable>(true);
                if (selectable != null)
                    found = grid.GetItemForSlotGO(selectable.gameObject);
            }
        }

        if (found == null)
            found = ResolveItemForSlotRoot(selectedGO);

        if (found != null)
            LastSelectedItem = found;
    }

    /// <summary>
    /// Debounced toggle to avoid open/close oscillation when multiple input events fire in the same frame
    /// or in quick succession (keyboard repeat, UI+Player maps both bound, etc.).
    /// </summary>
    public void Toggle()
    {
        // Frame guard: if two events hit this frame, ignore the 2nd
        if (Time.frameCount == _lastToggleFrame) return;

        // Cooldown guard: ignore rapid repeats
        if (Time.unscaledTime < _nextToggleTime) return;

        _lastToggleFrame = Time.frameCount;
        _nextToggleTime  = Time.unscaledTime + toggleDebounceSeconds;

        SetOpen(!IsOpen);
    }

    IEnumerator CloseAfterSlide()
    {
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime; // real time, works while paused
            yield return null;
        }

        if (inventoryPanel && inventoryPanel.activeSelf)
            inventoryPanel.SetActive(false);

        // Restore time ONLY if this menu paused it and Pause menu is not open
        if (pauseWhileOpen && _changedTimeScale && !GamePause.IsPaused)
        {
            Time.timeScale = (_prevTimeScale <= 0f) ? 1f : _prevTimeScale;
            _changedTimeScale = false;
        }

        IsOpen = false;
        closeRoutine = null;
    }
    // ---------------- Inline Autofocus ----------------
    IEnumerator FocusFirstOwnedAfterOpen()
    {
        // Let the panel and layout build
        yield return null;
        yield return new WaitForEndOfFrame();

        // Try to find first owned slot in section order
        GameObject owned = GetFirstOwnedSelectableGO(sectionCurrencyGrid)
                        ?? GetFirstOwnedSelectableGO(sectionFoodGrid)
                        ?? GetFirstOwnedSelectableGO(sectionItemsGrid);

        if (owned != null)
        {
            yield return EnsureSelectionCoroutine(owned);
            yield break;
        }

        // Fallback: first selectable so navigation works
        var first = GetFirstSelectableGO(sectionCurrencyGrid)
                 ?? GetFirstSelectableGO(sectionFoodGrid)
                 ?? GetFirstSelectableGO(sectionItemsGrid);
        if (first != null)
            yield return EnsureSelectionCoroutine(first);
    }

    GameObject GetFirstOwnedSelectableGO(Transform section)
    {
        if (!section || inventory == null) return null;

        if (grid != null)
        {
            var owned = grid.GetFirstOwnedInSection(section, inventory);
            if (owned) return GetSelectableGO(owned) ?? owned;
        }

        // Manual scan: check each child for a Selectable and bound ItemSO
        for (int i = 0; i < section.childCount; i++)
        {
            var child = section.GetChild(i);
            if (!child.gameObject.activeInHierarchy) continue;
            var selGO = GetSelectableGO(child.gameObject);
            if (!selGO) continue;

            var item = ResolveItemForSlotRoot(selGO) ?? ResolveItemForSlotRoot(child.gameObject);
            if (item == null) continue;
            if (inventory.CountOf(item) > 0) return selGO;
        }
        return null;
    }

    GameObject GetFirstSelectableGO(Transform section)
    {
        if (!section) return null;
        for (int i = 0; i < section.childCount; i++)
        {
            var child = section.GetChild(i);
            if (!child.gameObject.activeInHierarchy) continue;
            var selGO = GetSelectableGO(child.gameObject);
            if (selGO) return selGO;
        }
        return null;
    }

    GameObject GetSelectableGO(GameObject root)
    {
        if (!root) return null;
        var sel = root.GetComponent<Selectable>() ?? root.GetComponentInChildren<Selectable>(true);
        return sel ? sel.gameObject : null;
    }

    ItemSO ResolveItemForSlotRoot(GameObject slotRoot)
    {
        if (!slotRoot) return null;

        if (grid != null)
        {
            var mapped = grid.GetItemForSlotGO(slotRoot);
            if (mapped != null) return mapped;

            var selectable = slotRoot.GetComponent<Selectable>() ?? slotRoot.GetComponentInChildren<Selectable>(true);
            if (selectable != null)
            {
                mapped = grid.GetItemForSlotGO(selectable.gameObject);
                if (mapped != null) return mapped;
            }
        }

        return ResolveItemViaReflection(slotRoot);
    }

    ItemSO ResolveItemViaReflection(GameObject slotRoot)
    {
        if (!slotRoot) return null;

        var behaviours = slotRoot.GetComponents<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            var so = GetItemViaReflection(behaviour);
            if (so != null) return so;
        }

        var childBehaviour = slotRoot.GetComponentInChildren<MonoBehaviour>(true);
        if (childBehaviour != null)
        {
            var so = GetItemViaReflection(childBehaviour);
            if (so != null) return so;
        }

        return null;
    }

    ItemSO GetItemViaReflection(MonoBehaviour mb)
    {
        if (mb == null) return null;

        var type = mb.GetType();
        var prop = type.GetProperty("Item", ItemBindingFlags);
        if (prop != null && typeof(ItemSO).IsAssignableFrom(prop.PropertyType))
            return prop.GetValue(mb, null) as ItemSO;
        var field = type.GetField("Item", ItemBindingFlags);
        if (field != null && typeof(ItemSO).IsAssignableFrom(field.FieldType))
            return field.GetValue(mb) as ItemSO;
        return null;
    }

    IEnumerator EnsureSelectionCoroutine(GameObject target)
    {
        if (!target || EventSystem.current == null) yield break;
        var sel = target.GetComponent<Selectable>() ?? target.GetComponentInChildren<Selectable>(true);
        if (sel == null || !sel.IsActive() || !sel.interactable) yield break;

        // Try a few frames to latch
        for (int i = 0; i < 3; i++)
        {
            EventSystem.current.SetSelectedGameObject(null);
            sel.Select();
            if (EventSystem.current.currentSelectedGameObject != sel.gameObject)
                EventSystem.current.SetSelectedGameObject(sel.gameObject);
            if (EventSystem.current.currentSelectedGameObject == sel.gameObject)
                yield break;
            yield return null;
        }
    }
}
