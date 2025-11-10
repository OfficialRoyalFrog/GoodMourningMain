using UnityEngine;
using UnityEngine.InputSystem;
using KD.Core;

public class InputRouter : MonoBehaviour
{
    private PlayerControls _input;
    private GamePause _pause;
    private BuildModeController _build;
    private InventoryMenu _inventory;
    private InteractHoldController _hold;
    private InputAction _interactAction;

    [Header("Input Debounce")]
    [SerializeField] private float inventoryDebounceSeconds = 0.20f;
    private int _lastInventoryFrame = -1;
    private float _nextInventoryTime = 0f;

    void Awake()
    {
        _input = new PlayerControls();
        _pause = GetComponent<GamePause>();
        _build = GetComponent<BuildModeController>();
        _inventory = GetComponent<InventoryMenu>();
        _hold = GetComponent<InteractHoldController>();
#if UNITY_2023_1_OR_NEWER
        if (_hold == null) _hold = Object.FindFirstObjectByType<InteractHoldController>();
#else
        if (_hold == null) _hold = Object.FindObjectOfType<InteractHoldController>();
#endif
    }

    void OnEnable()
    {
        _input.Enable();
        _input.UI.Pause.performed += OnPausePerformed;
        _input.Player.BuildMode.performed += OnBuildPerformed;
        _input.UI.Inventory.started += OnInventoryStarted;
        try { _input.UI.Cancel.performed += OnCancelPerformed; } catch { }
        // Locate the Interact action by name (map-agnostic)
        try
        {
            _interactAction = ResolveInteractAction();
            if (_interactAction != null)
            {
                _interactAction.started   += OnInteractStarted;   // Press interactions
                _interactAction.performed += OnInteractStarted;   // Press(Release Only) or performed-on-press
                _interactAction.canceled  += OnInteractCanceled;  // Release
            }
        }
        catch { _interactAction = null; }
    }

    void OnDisable()
    {
        _input.UI.Pause.performed -= OnPausePerformed;
        _input.Player.BuildMode.performed -= OnBuildPerformed;
        _input.UI.Inventory.started -= OnInventoryStarted;
        try { _input.UI.Cancel.performed -= OnCancelPerformed; } catch { }

        if (_interactAction != null)
        {
            try
            {
                _interactAction.started   -= OnInteractStarted;
                _interactAction.performed -= OnInteractStarted;
                _interactAction.canceled  -= OnInteractCanceled;
            }
            catch { }
            _interactAction = null;
        }

        _input.Disable();
    }

    private InputAction ResolveInteractAction()
    {
        if (_input == null || _input.asset == null) return null;

        // Prefer the correctly spelled action name, but fall back to the legacy typo so legacy assets keep working.
        var action = _input.asset.FindAction("Interact", false);
        if (action != null) return action;

        action = _input.asset.FindAction("Interect", false);
#if UNITY_EDITOR
        if (action != null)
            Debug.LogWarning("Interact action not found; falling back to legacy 'Interect' binding. Rename the action to 'Interact' to silence this warning.", this);
#endif
        return action;
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (InventoryMenu.IsOpen)
        {
            if (_inventory != null)
                _inventory.SetOpen(false);
            return;
        }
        _pause.SetPaused(!GamePause.IsPaused);
    }

    private void OnBuildPerformed(InputAction.CallbackContext ctx)
    {
        _build.ToggleBuildMode();
    }

    private void OnInteractStarted(InputAction.CallbackContext ctx)
    {
        if (_hold != null)
            _hold.BeginHold();
    }

    private void OnInteractCanceled(InputAction.CallbackContext ctx)
    {
        if (_hold != null)
            _hold.CancelHold();
    }

    private void OnInventoryStarted(InputAction.CallbackContext ctx)
    {
        if (Time.frameCount == _lastInventoryFrame) return;
        if (Time.unscaledTime < _nextInventoryTime) return;

        _lastInventoryFrame = Time.frameCount;
        _nextInventoryTime = Time.unscaledTime + inventoryDebounceSeconds;

        if (_inventory != null)
            _inventory.Toggle();
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (_inventory != null && InventoryMenu.IsOpen)
        {
            _inventory.SetOpen(false);
            return;
        }
        // Optional: forward to Pause if desired
        // _pause.SetPaused(!GamePause.IsPaused);
    }
}
