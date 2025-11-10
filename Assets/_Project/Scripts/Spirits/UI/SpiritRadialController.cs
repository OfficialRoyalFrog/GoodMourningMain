using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SpiritRadialController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;   // assign SpiritRadial's CanvasGroup
    [SerializeField] private Button backdropButton;     // assign Backdrop Button (click to close)
    [SerializeField] private RectTransform ringRoot;    // assign RingRoot
    [SerializeField] private TextMeshProUGUI title;     // assign Label_Title
    [SerializeField] private Transform categoriesRoot;  // assign Categories container
    [SerializeField] private Transform actionsRoot;     // assign Actions container (hidden initially)
    [SerializeField] private SpiritRadialActionSpawner actionSpawner; // assign on SpiritRadial (Scene)
    [SerializeField] private RadialCameraFocus cameraFocus;   // optional; applies a subtle zoom & reframe while open
    [SerializeField] private Transform playerTransform;        // player to include in camera framing (auto-found if null)

    [Header("State (runtime)")]
    [SerializeField] private string currentSpiritId;
    [SerializeField] private string currentCategoryId;  // "Work" or "Interact" for now

    [Header("Freeze While Open")]
    [SerializeField] private PlayerMover playerMover;            // drag your Player's PlayerMover here
    [SerializeField] private PlayerInteractor playerInteractor;  // drag your PlayerInteractor here
    private bool frozePlayer = false;
    [Header("Freeze Spirit While Open")]
    private SpiritAgent frozenAgent; // the target spirit's agent we disable while the radial is open

    [Header("Positioning")]
    [SerializeField] private Canvas canvas;                 // auto-found if left null
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.4f, 0f); // lift above spirit head
    [SerializeField] private Vector2 screenOffset = Vector2.zero;
    [SerializeField] private bool followTargetWhileOpen = true;
    [SerializeField] private float clampMargin = 24f;

    private Transform targetTransform; // spirit we’re anchoring to

    // Global guard so gameplay input can block interactions while radial is open
    private static bool s_anyOpen = false;
    public static bool AnyOpen => s_anyOpen;

    void Awake()
    {
        // optional: auto-find if not assigned
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!title) title = GetComponentInChildren<TextMeshProUGUI>(true);
        HideImmediate();

        if (backdropButton)
        {
            backdropButton.onClick.RemoveAllListeners();
            backdropButton.onClick.AddListener(Hide);
        }

        // Auto-find player transform if not assigned
        if (!playerTransform)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) playerTransform = p.transform;
        }

        // Auto-find camera focus if not assigned
        if (!cameraFocus)
        {
            cameraFocus = GetComponent<RadialCameraFocus>();
            if (!cameraFocus)
                cameraFocus = GetComponentInParent<RadialCameraFocus>();
        }
    }

    // -- public API --

    public void Show(string spiritId)
    {
        if (s_anyOpen) return; // already open; ignore duplicate opens
        currentSpiritId = spiritId;

        // base UI state: show categories, hide actions
        if (title) title.text = "Select";
        if (categoriesRoot) categoriesRoot.gameObject.SetActive(true);
        if (actionsRoot) actionsRoot.gameObject.SetActive(false);

        SetVisible(true);
        // Freeze player input/movement while radial is open
        if (!frozePlayer)
        {
            if (playerMover)      playerMover.enabled = false;
            if (playerInteractor) playerInteractor.enabled = false;
            frozePlayer = true;
        }
        // Freeze the targeted spirit's wander while open
        frozenAgent = ResolveAgentFor(currentSpiritId);
        if (frozenAgent)
            frozenAgent.enabled = false;

        // Resolve the target transform (prefer frozen agent)
        targetTransform = null;
        if (frozenAgent) targetTransform = frozenAgent.transform;
        if (!targetTransform)
        {
#if UNITY_2022_3_OR_NEWER
            var handles = FindObjectsByType<SpiritIdHandle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var handles = FindObjectsOfType<SpiritIdHandle>(true);
#endif
            for (int i = 0; i < handles.Length; i++)
            {
                if (handles[i] && handles[i].Id == currentSpiritId)
                {
                    targetTransform = handles[i].transform;
                    break;
                }
            }
        }
        // Position now
        PositionRingOverTarget();

        Debug.Log($"[RadialController] CameraFocus? {(cameraFocus?"yes":"no")}, player={(playerTransform?playerTransform.name:"null")}, target={(targetTransform?targetTransform.name:"null")}");
        // Apply camera reframe (player + spirit) while the radial is open
        if (cameraFocus && playerTransform && targetTransform)
            cameraFocus.Apply(playerTransform, targetTransform);
    }

    public void Hide()
    {
        if (actionSpawner) actionSpawner.Clear();
        // Unfreeze player on close
        if (frozePlayer)
        {
            if (playerMover)      playerMover.enabled = true;
            if (playerInteractor) playerInteractor.enabled = true;
            frozePlayer = false;
        }
        // Re-enable spirit agent if we froze it
        if (frozenAgent)
        {
            frozenAgent.enabled = true;
            frozenAgent = null;
        }

        Debug.Log("[RadialController] Clearing camera focus");
        // Restore camera state
        if (cameraFocus)
            cameraFocus.Clear();

        SetVisible(false);
        currentSpiritId = null;
        currentCategoryId = null;
    }

    public void OpenCategory(string categoryId)
    {
        currentCategoryId = categoryId;
        if (title) title.text = categoryId;

        if (categoriesRoot) categoriesRoot.gameObject.SetActive(false);
        if (actionsRoot) actionsRoot.gameObject.SetActive(true);

        if (actionSpawner)
            actionSpawner.Populate(categoryId, currentSpiritId);
    }

    public void BackToCategories()
    {
        if (actionSpawner) actionSpawner.Clear();
        currentCategoryId = null;
        if (title) title.text = "Select";
        if (actionsRoot) actionsRoot.gameObject.SetActive(false);
        if (categoriesRoot) categoriesRoot.gameObject.SetActive(true);
    }

    // Convenience no-arg handlers for Button OnClick wiring
    public void OpenWork()     => OpenCategory("Work");
    public void OpenInteract() => OpenCategory("Interact");

    // -- internals --

    void SetVisible(bool v)
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = v ? 1f : 0f;
        canvasGroup.interactable = v;
        canvasGroup.blocksRaycasts = v;
        s_anyOpen = v;
    }

    void HideImmediate()
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private SpiritAgent ResolveAgentFor(string spiritId)
    {
        if (string.IsNullOrEmpty(spiritId)) return null;
#if UNITY_2022_3_OR_NEWER
        var agents = FindObjectsByType<SpiritAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var agents = FindObjectsOfType<SpiritAgent>(true);
#endif
        for (int i = 0; i < agents.Length; i++)
        {
            var handle = agents[i].GetComponentInChildren<SpiritIdHandle>(true);
            if (handle && handle.Id == spiritId)
                return agents[i];
        }
        return null;
    }

    void PositionRingOverTarget()
    {
        if (!IsOpen || ringRoot == null) return;

        // Ensure canvas reference
        if (!canvas)
            canvas = GetComponentInParent<Canvas>();
        var canvasRT = canvas ? canvas.transform as RectTransform : null;
        if (!canvasRT) return;

        // Choose anchor position
        Vector3 worldPos;
        if (targetTransform)
        {
            var ib = targetTransform.GetComponentInParent<InteractableBase>();
            if (ib != null) worldPos = ib.GetUIAnchorPosition() + worldOffset;
            else            worldPos = targetTransform.position + worldOffset;
        }
        else
        {
            // fallback: center
            worldPos = Vector3.zero;
        }

        // Convert world → canvas local point
        var cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
        Vector2 screen = Camera.main ? (Vector2)Camera.main.WorldToScreenPoint(worldPos) : Vector2.zero;
        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen + screenOffset, cam, out local))
        {
            // Clamp inside canvas so it doesn't clip
            local = ClampToCanvas(local, canvasRT, ringRoot.sizeDelta * 0.5f, clampMargin);
            ringRoot.anchoredPosition = local;
        }
    }

    static Vector2 ClampToCanvas(Vector2 local, RectTransform canvasRT, Vector2 halfSize, float margin)
    {
        var r = canvasRT.rect; // local space rect, centered around (0,0)
        float minX = r.xMin + margin + halfSize.x;
        float maxX = r.xMax - margin - halfSize.x;
        float minY = r.yMin + margin + halfSize.y;
        float maxY = r.yMax - margin - halfSize.y;
        return new Vector2(Mathf.Clamp(local.x, minX, maxX), Mathf.Clamp(local.y, minY, maxY));
    }

    void Update()
    {
        if (!IsOpen) return;

        // Keyboard: Esc backs from actions or closes from categories
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!string.IsNullOrEmpty(currentCategoryId)) BackToCategories();
            else Hide();
            return;
        }

        // Gamepad: B/Circle backs from actions or closes from categories
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            if (!string.IsNullOrEmpty(currentCategoryId)) BackToCategories();
            else Hide();
        }

        // Keep following target each frame while open
        if (followTargetWhileOpen)
            PositionRingOverTarget();
    }

    // Optional utility if you want to drive with keyboard later
    public bool IsOpen => canvasGroup && canvasGroup.alpha > 0.9f;
}