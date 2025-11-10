using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SpiritsListController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private SpiritDatabase database;
    [SerializeField] private SpiritManager spiritManager;

    [Header("UI")]
    [SerializeField] private Transform listRoot;     // Panel_Cult/Panel_SpiritsList/ListRoot
    [SerializeField] private GameObject cellPrefab;  // Cell_SpiritCard prefab
    [SerializeField] private TextMeshProUGUI countText; // Panel_Cult/Panel_SpiritsList/SummaryRow/CountText

    private readonly List<GameObject> spawned = new();

    void OnEnable()
    {
        if (spiritManager)
        {
            spiritManager.OnOwnedChanged += Refresh;
            spiritManager.OnStatesChanged += Refresh; // refresh when meters/xp/days change
        }
        Refresh();
    }

    void OnDisable()
    {
        if (spiritManager)
        {
            spiritManager.OnOwnedChanged -= Refresh;
            spiritManager.OnStatesChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        // clear
        for (int i = spawned.Count - 1; i >= 0; i--) if (spawned[i]) Destroy(spawned[i]);
        spawned.Clear();

        if (!database || database.AllSpirits == null || listRoot == null) return;

        int total = database.AllSpirits.Count;
        int owned = 0;

        foreach (var so in database.AllSpirits)
        {
            if (!so) continue;

            // OWNED-ONLY: skip locked/pending spirits entirely
            bool isOwned = spiritManager && spiritManager.Has(so.Id);
            if (!isOwned) continue;
            owned++;

            var card = Instantiate(cellPrefab, listRoot, false);
            spawned.Add(card);

            // ----- bind fields (paths must match your prefab) -----
            var portrait = card.transform.Find("Portrait")?.GetComponent<Image>();
            var nameTxt  = card.transform.Find("Right/Row_NameLevel/Name")?.GetComponent<TextMeshProUGUI>();
            var levelTxt = card.transform.Find("Right/Row_NameLevel/Level")?.GetComponent<TextMeshProUGUI>();
            var xpFill   = card.transform.Find("Right/Row_XP_Stats/XPBar/XPBar_Fill")?.GetComponent<Image>();
            var hunger   = card.transform.Find("Right/Row_XP_Stats/Circle_Hunger/Fill")?.GetComponent<Image>();
            var sick     = card.transform.Find("Right/Row_XP_Stats/Circle_Sickness/Fill")?.GetComponent<Image>();
            var member   = card.transform.Find("Right/Row_MemberTime/Label")?.GetComponent<TextMeshProUGUI>();

            if (portrait) portrait.sprite = so.Portrait;

            // -------- RUNTIME STATE (real data) --------
            int level = 1;
            float xp01 = 0f;
            float serenity01 = 0.5f;
            float appetite01 = 1f;
            float integrity01 = 0.5f;
            int daysOwned = 0;

            if (spiritManager != null && spiritManager.TryGetState(so.Id, out var st) && st != null)
            {
                level       = Mathf.Max(1, st.level);
                xp01        = Mathf.Clamp01(st.xp01);
                serenity01  = Mathf.Clamp01(st.serenity01);
                appetite01  = Mathf.Clamp01(st.appetite01);
                integrity01 = Mathf.Clamp01(st.integrity01);
                daysOwned   = Mathf.Max(0, st.daysOwned);
            }

            // Title: "DisplayName - Lvl VII"
            if (nameTxt)
            {
                string display = string.IsNullOrWhiteSpace(so.DisplayName) ? so.name : so.DisplayName;
                nameTxt.text = $"{display} - Lvl {ToRoman(level)}";
            }
            if (levelTxt) levelTxt.text = string.Empty; // keep the separate label hidden

            // XP bar (0..1)
            if (xpFill) xpFill.fillAmount = xp01;

            // Meters:
            // Hunger circle shows appetite (more fill = more fed)
            if (hunger) hunger.fillAmount = appetite01;

            // Wellness (full = healthy), mapped directly from integrity01
            if (sick) sick.fillAmount = integrity01;

            // Member days
            if (member) member.text = (daysOwned <= 1) ? "Member for 1 day" : $"Member for {daysOwned} days";

            // make first card selectable for auto-focus
            var selectable = card.GetComponent<Selectable>();
            if (!selectable)
            {
                selectable = card.AddComponent<Button>();
                var targetImg = card.GetComponent<Image>();
                if (targetImg)
                {
                    var btn = selectable as Button;
                    btn.transition = Selectable.Transition.ColorTint;
                    var colors = btn.colors;
                    colors.normalColor = targetImg.color;
                    colors.highlightedColor = new Color(1f,1f,1f,0.12f);
                    colors.pressedColor = new Color(1f,1f,1f,0.18f);
                    colors.selectedColor = new Color(1f,1f,1f,0.10f);
                    colors.disabledColor = new Color(1f,1f,1f,0.05f);
                    btn.colors = colors;
                }
            }
        }

        // update summary label
        if (countText) countText.text = $"{owned}/{total}";

        // auto-select first card for controller/keyboard
        if (spawned.Count > 0)
        {
            var first = spawned[0].GetComponent<Selectable>();
            if (first)
            {
                EventSystem.current?.SetSelectedGameObject(null);
                first.Select();
            }
        }

        // force layout so the outer scroll can size immediately
        Canvas.ForceUpdateCanvases();
        var rt = listRoot as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }


    static string ToRoman(int number)
    {
        if (number <= 0) return "I";
        (int val, string sym)[] map = new (int, string)[] {
            (1000,"M"),(900,"CM"),(500,"D"),(400,"CD"),
            (100,"C"),(90,"XC"),(50,"L"),(40,"XL"),
            (10,"X"),(9,"IX"),(5,"V"),(4,"IV"),(1,"I")
        };
        int n = number;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var m in map)
        {
            while (n >= m.val) { sb.Append(m.sym); n -= m.val; }
            if (n == 0) break;
        }
        return sb.ToString();
    }
}