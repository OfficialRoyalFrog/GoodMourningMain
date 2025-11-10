using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryTabsController : MonoBehaviour
{
    public enum Tab
    {
        Inventory = 0,
        Player    = 1,
        Cult      = 2,
        Quests    = 3
    }

    [Header("Tab Buttons (top row)")]
    [SerializeField] private Button tabInventory;
    [SerializeField] private Button tabPlayer;
    [SerializeField] private Button tabCult;
    [SerializeField] private Button tabQuests;

    [Header("Content Panels (beneath tabs)")]
    [SerializeField] private RectTransform panelInventory;   // Content/Panel_Inventory
    [SerializeField] private RectTransform panelPlayer;      // Content/Panel_Player
    [SerializeField] private RectTransform panelCult;        // Content/Panel_Cult
    [SerializeField] private RectTransform panelQuests;      // Content/Panel_Quests

    [Header("Visuals")]
    [Tooltip("Optional: highlight style for the active tab label (e.g., bold/white).")]
    [SerializeField] private Color activeColor   = Color.white; // selected tab
    [SerializeField] private Color inactiveColor = Color.black; // default tab
    [Tooltip("If your tab button has a TMP child, we’ll tint it on select.")]
    [SerializeField] private bool tintTabLabel = true;

    [Header("Sprites")]
    [Tooltip("Inactive tab background (e.g., grey)")]
    [SerializeField] private Sprite inactiveSprite;
    [Tooltip("Active tab background (e.g., red)")]
    [SerializeField] private Sprite activeSprite;
    [Tooltip("Swap the tab button Image sprite when a tab is selected")]
    [SerializeField] private bool swapTabSprite = true;

    [Header("Size")]
    [Tooltip("If true, the selected tab will be slightly taller than inactive tabs.")]
    [SerializeField] private bool emphasizeSelectedHeight = true;
    [Tooltip("Preferred height for inactive tabs (must be respected by LayoutElement/HorizontalLayoutGroup).")]
    [SerializeField] private float normalHeight = 100f;
    [Tooltip("Preferred height for the active tab.")]
    [SerializeField] private float selectedHeight = 110f;

    [Header("Initial State")]
    [SerializeField] private Tab initialTab = Tab.Inventory;

    // Keep arrays for easy indexing
    private Button[] _buttons;
    private RectTransform[] _panels;
    private Tab _current;

    void Awake()
    {
        _buttons = new[] { tabInventory, tabPlayer, tabCult, tabQuests };
        _panels  = new[] { panelInventory, panelPlayer, panelCult, panelQuests };

        // Wire clicks
        if (tabInventory) tabInventory.onClick.AddListener(() => Show(Tab.Inventory));
        if (tabPlayer)    tabPlayer.onClick.AddListener(() => Show(Tab.Player));
        if (tabCult)      tabCult.onClick.AddListener(() => Show(Tab.Cult));
        if (tabQuests)    tabQuests.onClick.AddListener(() => Show(Tab.Quests));
    }

    void OnEnable()
    {
        // Ensure consistent state each time the panel appears
        Show(initialTab, force: true);
    }

    /// <summary>Switch to the given tab (enables one panel, disables the rest).</summary>
    public void Show(Tab tab, bool force = false)
    {
        if (!force && tab == _current) return;
        _current = tab;

        for (int i = 0; i < _panels.Length; i++)
        {
            bool isActive = (i == (int)tab);

            // Panel show/hide (CanvasGroup + SetActive for safety)
            var p = _panels[i];
            if (p)
            {
                var cg = p.GetComponent<CanvasGroup>();
                if (cg)
                {
                    cg.alpha = isActive ? 1f : 0f;
                    cg.interactable = isActive;
                    cg.blocksRaycasts = isActive;
                }
                if (p.gameObject.activeSelf != isActive)
                    p.gameObject.SetActive(isActive);
            }

            // Tab label tinting
            var b = _buttons[i];
            if (b && tintTabLabel)
            {
                var tmp = b.GetComponentInChildren<TMP_Text>(true);
                if (tmp) tmp.color = isActive ? activeColor : inactiveColor;
            }

            // Tab background sprite swap (grey <-> red)
            if (swapTabSprite)
            {
                var btnImg = b ? b.GetComponent<Image>() : null;
                if (btnImg && activeSprite && inactiveSprite)
                {
                    btnImg.sprite = isActive ? activeSprite : inactiveSprite;
                    // If your sprites are 9-sliced, ensure sliced type to avoid distortion
                    if (btnImg.type != Image.Type.Sliced)
                        btnImg.type = Image.Type.Sliced;
                }
            }

            // Selected tab height emphasis
            if (emphasizeSelectedHeight)
            {
                if (b)
                {
                    var layout = b.GetComponent<LayoutElement>();
                    if (!layout)
                    {
                        // Ensure a LayoutElement exists so the HorizontalLayoutGroup can respect per-tab height
                        layout = b.gameObject.AddComponent<LayoutElement>();
                        layout.minHeight = normalHeight; // optional safety
                    }
                    layout.preferredHeight = isActive ? selectedHeight : normalHeight;
                }
            }
        }
    }

    // Optional: expose a quick API for other scripts
    public void ShowInventory() => Show(Tab.Inventory);
    public void ShowPlayer()    => Show(Tab.Player);
    public void ShowCult()      => Show(Tab.Cult);
    public void ShowQuests()    => Show(Tab.Quests);

    public void SetTabSprites(Sprite inactive, Sprite active)
    {
        inactiveSprite = inactive;
        activeSprite = active;
        // Refresh current selection visuals
        Show(_current, force: true);
    }
}