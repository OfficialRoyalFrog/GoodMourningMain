using UnityEngine;
using UnityEngine.UI;

public class CultPanelNavigator : MonoBehaviour
{
    [Header("Headers (CanvasGroups)")]
    [SerializeField] private CanvasGroup headerOverview;    // Panel_Cult/Header row (NOT inside scroll area)
    [SerializeField] private CanvasGroup headerSpirits;     // Panel_Cult/Spirits header (e.g., Panel_SpiritsList/HeaderRow as its own CG OR a separate header object)
    [SerializeField] private CanvasGroup backButtonGroup;   // Sidebar/Btn_Back (CanvasGroup)
    [SerializeField] private CanvasGroup tabsRowGroup;      // Sidebar/TabsRow (CanvasGroup)

    [Header("Roots (ScrollView groups)")]
    [SerializeField] private CanvasGroup overviewRoot;   // Sidebar/ScrollView_Overview (CanvasGroup on root)
    [SerializeField] private CanvasGroup spiritsRoot;    // Sidebar/ScrollView_Spirits (CanvasGroup on root)

    [Header("Panels (CanvasGroups)")]
    [SerializeField] private CanvasGroup panelOverview;     // Panel_Cult/Body (has its own CanvasGroup)
    [SerializeField] private CanvasGroup panelSpiritsList;  // Panel_Cult/Panel_SpiritsList (has its own CanvasGroup)

    [Header("Buttons")]
    [SerializeField] private Button btnOpenSpirits;         // Panel_Cult/Body/Row_SpiritsButton/Button_Spirits
    [SerializeField] private Button btnBack;                // Sidebar/Btn_Back (Button)

    private void Awake()
    {
        if (btnOpenSpirits != null)
        {
            btnOpenSpirits.onClick.RemoveAllListeners();
            btnOpenSpirits.onClick.AddListener(ShowSpirits);
        }

        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(ShowOverview); // ensure only one subscription
        }
    }

    private void OnEnable()
    {
        // Default to Overview when the panel becomes active
        ShowOverview();
    }

    public void ShowOverview()
    {
        // Headers
        SetVisible(headerOverview,  true);
        SetVisible(headerSpirits,   false);
        SetVisible(backButtonGroup, false);
        SetVisible(tabsRowGroup, true);

        // Root groups (separate scroll views)
        SetVisible(overviewRoot, true);
        SetVisible(spiritsRoot,  false);

        // Panels
        SetVisible(panelOverview,   true);
        SetVisible(panelSpiritsList,false);
    }

    public void ShowSpirits()
    {
        // Headers
        SetVisible(headerOverview,  false);
        SetVisible(headerSpirits,   true);
        SetVisible(backButtonGroup, true);
        SetVisible(tabsRowGroup, false);

        // Root groups (separate scroll views)
        SetVisible(overviewRoot, false);
        SetVisible(spiritsRoot,  true);

        // Panels
        SetVisible(panelOverview,   false);
        SetVisible(panelSpiritsList,true);
    }

    // Hook this to any top navigation tab button to ensure Back hides when leaving Spirits
    public void ShowOverviewExternal()
    {
        ShowOverview();
    }

    private void SetVisible(CanvasGroup cg, bool on)
    {
        if (cg == null) return;
        // Toggle GameObject active so it stops/starts participating in layout
        if (cg.gameObject.activeSelf != on)
            cg.gameObject.SetActive(on);

        // Keep CG properties coherent (useful if you later keep both active)
        cg.alpha       = on ? 1f : 0f;
        cg.interactable= on;
        cg.blocksRaycasts = on;
    }
}