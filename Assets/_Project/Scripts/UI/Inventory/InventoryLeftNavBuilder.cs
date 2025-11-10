using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)] // runs after layout
public class InventoryLeftNavBuilder : MonoBehaviour
{
    [Header("Section Grids (top → bottom)")]
    [SerializeField] private Transform sectionCurrencyGrid;
    [SerializeField] private Transform sectionFoodGrid;
    [SerializeField] private Transform sectionItemsGrid;

    [Header("Grid Settings")]
    [Tooltip("Leave 0 to read from each GridLayoutGroup; override to force same column count.")]
    [SerializeField] private int overrideColumns = 0;

    [Header("Wrap Behavior")]
    [SerializeField] private bool wrapHorizontal = true;
    [SerializeField] private bool wrapVerticalAcrossSections = true;

    [Header("Auto Rebuild")]
    [SerializeField] private bool rebuildOnEnable = true;
    [SerializeField] private bool rebuildOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool logAssignments = false;

    private readonly List<Selectable> allSlots = new();
    private readonly List<int> sectionStarts = new();
    private readonly List<int> sectionCounts = new();
    private readonly List<int> sectionCols = new();

    private void OnEnable()
    {
        if (rebuildOnEnable)
            Rebuild();
    }

    private void Start()
    {
        if (rebuildOnStart)
            Rebuild();
    }

    public void Rebuild()
    {
        allSlots.Clear();
        sectionStarts.Clear();
        sectionCounts.Clear();
        sectionCols.Clear();

        var grids = new List<Transform>();
        if (sectionCurrencyGrid) grids.Add(sectionCurrencyGrid);
        if (sectionFoodGrid) grids.Add(sectionFoodGrid);
        if (sectionItemsGrid) grids.Add(sectionItemsGrid);

        int running = 0;
        foreach (var grid in grids)
        {
            sectionStarts.Add(running);

            int cols = overrideColumns > 0 ? overrideColumns : GetCols(grid);
            sectionCols.Add(cols);

            int added = 0;
            for (int i = 0; i < grid.childCount; i++)
            {
                var c = grid.GetChild(i);
                if (!c.gameObject.activeInHierarchy) continue;
                if (c.TryGetComponent<Selectable>(out var sel) && sel.interactable)
                {
                    allSlots.Add(sel);
                    added++;
                    running++;
                }
            }
            sectionCounts.Add(added);
        }

        AssignExplicitNav();
    }

    private int GetCols(Transform grid)
    {
        var glg = grid.GetComponent<GridLayoutGroup>();
        if (glg && glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            return glg.constraintCount;
        return 7; // fallback
    }

    private void AssignExplicitNav()
    {
        if (allSlots.Count == 0) return;

        for (int g = 0; g < allSlots.Count; g++)
        {
            var (section, local) = GlobalToLocal(g);
            int cols = sectionCols[section];
            int count = sectionCounts[section];
            int row = local / cols;
            int col = local % cols;

            int li = GetLeft(section, row, col);
            int ri = GetRight(section, row, col);
            int ui = GetUp(section, row, col);
            int di = GetDown(section, row, col);

            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnLeft  = SafeSel(li);
            nav.selectOnRight = SafeSel(ri);
            nav.selectOnUp    = SafeSel(ui);
            nav.selectOnDown  = SafeSel(di);
            allSlots[g].navigation = nav;

            if (logAssignments)
            {
                Debug.Log($"[Nav] {NameOf(g)}  L:{NameOf(li)} R:{NameOf(ri)} U:{NameOf(ui)} D:{NameOf(di)}");
            }
        }
    }

    private (int sec, int local) GlobalToLocal(int global)
    {
        for (int s = 0; s < sectionStarts.Count; s++)
        {
            int start = sectionStarts[s];
            int count = sectionCounts[s];
            if (global >= start && global < start + count)
                return (s, global - start);
        }
        return (0, 0);
    }

    // LEFT: step to previous item; if at first item of a row, jump to previous row's last item;
    // if at very first item of section, jump to previous section's last item (or wrap to last section if enabled)
    private int GetLeft(int sec, int row, int col)
    {
        int cols  = sectionCols[sec];
        int count = sectionCounts[sec];

        int local = row * cols + col;        // current local index
        int prevLocal = local - 1;           // simply previous item in reading order

        if (prevLocal >= 0)
            return sectionStarts[sec] + prevLocal;

        // move to previous section's last selectable
        int prevSec = sec - 1;
        if (prevSec < 0) prevSec = wrapVerticalAcrossSections ? sectionStarts.Count - 1 : sec;

        int prevCount = sectionCounts[prevSec];
        if (prevCount > 0)
            return sectionStarts[prevSec] + (prevCount - 1);

        // no items in previous section; stay on current first (defensive)
        return sectionStarts[sec];
    }

    // RIGHT: step to next item; if at last item of a row, jump to next row's FIRST item;
    // if at the final item of a section, jump to FIRST of next section (or wrap to first section if enabled)
    private int GetRight(int sec, int row, int col)
    {
        int cols  = sectionCols[sec];
        int count = sectionCounts[sec];

        int local = row * cols + col;        // current local index
        int nextLocal = local + 1;           // simply next item in reading order

        if (nextLocal < count)
            return sectionStarts[sec] + nextLocal;

        // at row/section end
        if (!wrapHorizontal)
            return sectionStarts[sec] + Mathf.Max(0, count - 1); // clamp

        // move to next section's first selectable
        int nextSec = sec + 1;
        if (nextSec >= sectionStarts.Count) nextSec = wrapVerticalAcrossSections ? 0 : sec;

        int nextCount = sectionCounts[nextSec];
        if (nextCount > 0)
            return sectionStarts[nextSec];

        // no items in next section; stay on current last (defensive)
        return sectionStarts[sec] + Mathf.Max(0, count - 1);
    }

    // UP: previous row same column; if above first row, hop to previous section's last row same column (or wrap)
    private int GetUp(int sec, int row, int col)
    {
        int cols = sectionCols[sec];
        int count = sectionCounts[sec];

        int local = (row - 1) * cols + col;
        if (row > 0 && local >= 0)
        {
            local = Mathf.Clamp(local, 0, count - 1);
            return sectionStarts[sec] + local;
        }

        // previous section
        int prev = sec - 1;
        if (prev < 0) prev = wrapVerticalAcrossSections ? sectionStarts.Count - 1 : sec;

        int pCount = sectionCounts[prev];
        if (pCount <= 0) return sectionStarts[sec]; // stay

        int pCols = sectionCols[prev];
        int pRows = Mathf.CeilToInt(pCount / (float)pCols);
        int targetLocal = (pRows - 1) * pCols + Mathf.Min(col, pCols - 1);
        targetLocal = Mathf.Clamp(targetLocal, 0, pCount - 1);
        return sectionStarts[prev] + targetLocal;
    }

    // DOWN: next row same column; if beyond last row, hop to next section's first row same column (or wrap)
    private int GetDown(int sec, int row, int col)
    {
        int cols = sectionCols[sec];
        int count = sectionCounts[sec];

        int local = (row + 1) * cols + col;
        if (local < count)
            return sectionStarts[sec] + local;

        int next = sec + 1;
        if (next >= sectionStarts.Count) next = wrapVerticalAcrossSections ? 0 : sec;

        int nCount = sectionCounts[next];
        if (nCount <= 0) return sectionStarts[sec] + Mathf.Max(0, count - 1);

        int nCols = sectionCols[next];
        int targetLocal = Mathf.Min(col, nCols - 1);
        targetLocal = Mathf.Clamp(targetLocal, 0, nCount - 1);
        return sectionStarts[next] + targetLocal;
    }

    private Selectable SafeSel(int globalIndex)
    {
        if (globalIndex < 0 || globalIndex >= allSlots.Count) return null;
        return allSlots[globalIndex];
    }

    private string NameOf(int globalIndex)
    {
        var s = SafeSel(globalIndex);
        return s ? s.name : "—";
    }
}