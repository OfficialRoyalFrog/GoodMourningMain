using System.Collections.Generic;
using UnityEngine;

public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private RectTransform toastRoot;   // HUD_Canvas/Toasts
    [SerializeField] private ToastUI toastPrefab;       // ToastItem prefab

    [Header("Placement")]
    [Tooltip("Pixels from the left screen edge where the toast rests.")]
    public float leftMarginX = 24f;
    [Tooltip("Top Y (relative to toastRoot) for the first toast.")]
    public float topStartY = -16f;
    [Tooltip("Vertical spacing between rows (px).")]
    public float rowSpacing = 6f;

    [Header("Capacity")]
    [Min(1)] public int maxConcurrent = 5;

    [Header("Merging")]
    public float perMergeExtraHold = 0.6f;

    private readonly List<Entry> active = new(); // newest at end
    private readonly Dictionary<string, Entry> byKey = new();

    // ====== Notifications History (for UI) ======
    [System.Serializable]
    public struct ToastHistoryEntry
    {
        public Sprite icon;
        public string message;    // e.g., "+3 Wood"
        public int delta;         // 3
        public int totalAfter;    // 27
        public Color color;       // amount color (optional)
        public long utcTicks;     // for ordering/time display
    }

    [Header("History")]
    [SerializeField, Min(1)] private int historyCapacity = 50;
    private readonly List<ToastHistoryEntry> history = new List<ToastHistoryEntry>(50);

    public System.Action<ToastHistoryEntry> OnToastAdded; // fired when a toast is shown
    public IReadOnlyList<ToastHistoryEntry> History => history;

    private struct Entry
    {
        public string key;
        public ToastUI ui;
        public ItemSO item;
        public int runningAdd;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!toastRoot)
        {
            var t = transform.Find("Toasts") as RectTransform;
            if (t) toastRoot = t;
        }
    }

    private void PushHistory(Sprite icon, string itemName, int deltaAdded, int totalAfter, Color amountColor)
    {
        var entry = new ToastHistoryEntry
        {
            icon = icon,
            message = "+" + deltaAdded + " " + itemName,
            delta = deltaAdded,
            totalAfter = totalAfter,
            color = amountColor,
            utcTicks = System.DateTime.UtcNow.Ticks
        };

        if (history.Count >= Mathf.Max(1, historyCapacity))
            history.RemoveAt(0);
        history.Add(entry);

        OnToastAdded?.Invoke(entry);
    }

    // Public API: call AFTER Inventory.Add(item, amount) so totals are current
    public void ShowItemToast(ItemSO item, int deltaAdded)
    {
        if (!item || deltaAdded <= 0 || !toastPrefab || !toastRoot) return;

        string key = $"item:{item.Id}";
        int total = Inventory.Instance ? Inventory.Instance.CountOf(item) : deltaAdded;

        // MERGE if exists
        if (byKey.TryGetValue(key, out var existing))
        {
            existing.runningAdd += deltaAdded;
            existing.ui.MergeAmount(deltaAdded, total, perMergeExtraHold);
            PushHistory(item.Icon, item.DisplayName, deltaAdded, total, new Color(1f, 0.92f, 0.4f));
            byKey[key] = existing;
            return;
        }

        // Enforce capacity: if full, force earliest to leave quickly
        while (active.Count >= maxConcurrent)
        {
            var oldest = active[0];
            if (oldest.ui) oldest.ui.ForceEarlyExit(1.8f);
            active.RemoveAt(0);
            if (!string.IsNullOrEmpty(oldest.key)) byKey.Remove(oldest.key);
        }

        // Spawn new
        var ui = Instantiate(toastPrefab, toastRoot);
        // lock its pivot/anchor (safety)
        var rt = ui.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);

        // Set fields that ensure left landing
        ui.targetX = leftMarginX;

        // Determine Y slot (new rows go to the BOTTOM of the stack visually)
        float y = ComputeYForIndex(active.Count);
        ui.SetY(y);

        // Initialize content + animate
        ui.Setup(item.Icon, item.DisplayName, deltaAdded, total);
        PushHistory(item.Icon, item.DisplayName, deltaAdded, total, new Color(1f, 0.92f, 0.4f));

        // Track
        var entry = new Entry { key = key, ui = ui, item = item, runningAdd = deltaAdded };
        active.Add(entry);
        byKey[key] = entry;

        // Watch for destroy to keep dictionaries clean
        var cleaner = ui.gameObject.AddComponent<ToastCleaner>();
        cleaner.onDestroyed = () =>
        {
            // remove from active + byKey if this specific instance dies
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].ui == ui)
                {
                    if (!string.IsNullOrEmpty(active[i].key)) byKey.Remove(active[i].key);
                    active.RemoveAt(i);
                }
            }
            // reflow Y slots for remaining rows
            Reflow();
        };
    }

    private float ComputeYForIndex(int index)
    {
        // index 0 = top row (closest under hearts)
        float h = toastPrefab.GetComponent<RectTransform>().rect.height;
        return topStartY - index * (h + rowSpacing);
    }

    private void Reflow()
    {
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].ui)
                active[i].ui.SetY(ComputeYForIndex(i));
        }
    }

    private class ToastCleaner : MonoBehaviour
    {
        public System.Action onDestroyed;
        void OnDestroy() => onDestroyed?.Invoke();
    }
}