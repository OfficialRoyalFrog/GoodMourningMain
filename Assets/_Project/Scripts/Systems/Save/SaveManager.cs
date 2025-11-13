using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic; // NEW
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core.TimeSystem;

/// Save system backbone with versioned schema (v1, v2, v3).
/// v2 adds ownedSpiritIds (Spirit ownership) while remaining backward-compatible with v1.
/// v3 adds pendingSpiritIds (pending spirits) while remaining backward-compatible with v2.
[DisallowMultipleComponent]
public sealed class SaveManager : MonoBehaviour
{
    public static event Action<int> OnSlotChanged;   // fires when current slot changes
    public static event Action OnSaved;              // fires after a successful save
    public static event Action OnLoaded;             // fires after a successful load

    public int CurrentSlot => currentSlot;

    // ===== Singleton =====
    public static SaveManager Instance { get; private set; }

    [Header("File")]
    [SerializeField, Range(1, 3)] private int currentSlot = 1;
    [Tooltip("Writes JSON as-is (no compression).")]
    [SerializeField] private bool prettyPrint = false;

    // Full path we will write to (e.g., ~/Library/Application Support/.../save_slot1.json)
    public string PathFull => SaveSlots.GetPath(currentSlot);

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[SaveManager] Ready. Save path:\n{PathFull}");
    }

    // ---- Public API ----

    public void SaveNow() => _ = SaveAsync();
    public void LoadNow() => _ = LoadAsync();
    public bool HasSave() => File.Exists(PathFull);

    public void SetCurrentSlot(int slot)
    {
        currentSlot = Mathf.Clamp(slot, 1, 3);
        Debug.Log($"[SaveManager] Current save slot set to {currentSlot}");
        OnSlotChanged?.Invoke(currentSlot);
    }

    public bool HasSave(int slot)
    {
        var path = SaveSlots.GetPath(Mathf.Clamp(slot, 1, 3));
        return File.Exists(path);
    }

    public void SaveNow(int slot)
    {
        SetCurrentSlot(slot);
        _ = SaveAsync();
    }

    public void LoadNow(int slot)
    {
        SetCurrentSlot(slot);
        _ = LoadAsync();
    }

    public bool DeleteSlot(int slot)
    {
        slot = Mathf.Clamp(slot, 1, 3);
        var path = SaveSlots.GetPath(slot);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveManager] Deleted slot {slot} file: {path}");
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to delete slot {slot}: {e}");
        }
        return false;
    }

    // ====== Internal Models ======

    [Serializable]
    private class SaveDataV1
    {
        public int version = 1;
        public string scene;
        public long savedUtcTicks;
        public float playerX, playerY, playerZ, playerYaw;
        public int day, hour, minute;
    }

    // v2 inherits v1 and adds spirits; JsonUtility supports inherited serializable fields.
    [Serializable]
    private class SaveDataV2 : SaveDataV1
    {
        public List<string> ownedSpiritIds = new List<string>();

        public SaveDataV2()
        {
            version = 2;
        }
    }

    // v3 inherits v2 and adds pending spirits
    [Serializable]
    private class SaveDataV3 : SaveDataV2
    {
        public List<string> pendingSpiritIds = new List<string>();

        public SaveDataV3()
        {
            version = 3;
        }
    }

    // v4 inherits v3 and adds per-spirit runtime states (for OWNED spirits)
    [Serializable]
    private class SaveDataV4 : SaveDataV3
    {
        public List<SpiritRuntime.DTO> spiritStates = new List<SpiritRuntime.DTO>();

        public SaveDataV4()
        {
            version = 4;
        }
    }

    // tiny probe to sniff version without constructing the full object
    [Serializable]
    private struct VersionProbe { public int version; }

    // ====== Capture ======

    private SaveDataV2 CaptureV2()
    {
        var v2 = new SaveDataV2();

        // Scene
        v2.scene = SceneManager.GetActiveScene().name;
        v2.savedUtcTicks = DateTime.UtcNow.Ticks;

        // Player
        var player = GameObject.FindWithTag("Player");
        if (player)
        {
            var p = player.transform.position;
            v2.playerX = p.x; v2.playerY = p.y; v2.playerZ = p.z;
            v2.playerYaw = player.transform.eulerAngles.y;
        }

        // Time
        var tm = TimeManager.Instance;
        if (tm)
        {
            v2.day = tm.DayIndex;
            v2.hour = tm.Hour;
            v2.minute = tm.Minute;
        }

        // Spirits (safe if manager absent)
        var sm = SpiritManager.Instance;
        if (sm != null && sm.OwnedSpiritIds != null)
        {
            v2.ownedSpiritIds = new List<string>(sm.OwnedSpiritIds);
        }
        else
        {
            v2.ownedSpiritIds = new List<string>();
        }

        return v2;
    }

    private SaveDataV3 CaptureV3()
    {
        var v3 = new SaveDataV3();

        // Scene
        v3.scene = SceneManager.GetActiveScene().name;
        v3.savedUtcTicks = DateTime.UtcNow.Ticks;

        // Player
        var player = GameObject.FindWithTag("Player");
        if (player)
        {
            var p = player.transform.position;
            v3.playerX = p.x; v3.playerY = p.y; v3.playerZ = p.z;
            v3.playerYaw = player.transform.eulerAngles.y;
        }

        // Time
        var tm = TimeManager.Instance;
        if (tm)
        {
            v3.day = tm.DayIndex;
            v3.hour = tm.Hour;
            v3.minute = tm.Minute;
        }

        // Spirits (owned + pending)
        var sm = SpiritManager.Instance;
        if (sm != null)
        {
            // Owned
            if (sm.OwnedSpiritIds != null)
                v3.ownedSpiritIds = new List<string>(sm.OwnedSpiritIds);
            else
                v3.ownedSpiritIds = new List<string>();

            // Pending (requires SpiritManager.PendingIds)
            var pending = (sm.PendingIds != null) ? sm.PendingIds : Array.Empty<string>();
            v3.pendingSpiritIds = new List<string>(pending);
        }
        else
        {
            v3.ownedSpiritIds = new List<string>();
            v3.pendingSpiritIds = new List<string>();
        }

        return v3;
    }

    private SaveDataV4 CaptureV4()
    {
        var v4 = new SaveDataV4();

        // Scene
        v4.scene = SceneManager.GetActiveScene().name;
        v4.savedUtcTicks = DateTime.UtcNow.Ticks;

        // Player
        var player = GameObject.FindWithTag("Player");
        if (player)
        {
            var p = player.transform.position;
            v4.playerX = p.x; v4.playerY = p.y; v4.playerZ = p.z;
            v4.playerYaw = player.transform.eulerAngles.y;
        }

        // Time
        var tm = TimeManager.Instance;
        if (tm)
        {
            v4.day = tm.DayIndex;
            v4.hour = tm.Hour;
            v4.minute = tm.Minute;
        }

        // Spirits (owned + pending + runtime states)
        var sm = SpiritManager.Instance;
        if (sm != null)
        {
            // Owned
            if (sm.OwnedSpiritIds != null)
                v4.ownedSpiritIds = new List<string>(sm.OwnedSpiritIds);
            else
                v4.ownedSpiritIds = new List<string>();

            // Pending
            var pending = (sm.PendingIds != null) ? sm.PendingIds : Array.Empty<string>();
            v4.pendingSpiritIds = new List<string>(pending);

            // Runtime states (OWNED-only)
            v4.spiritStates = sm.CaptureStatesForOwned();
        }
        else
        {
            v4.ownedSpiritIds = new List<string>();
            v4.pendingSpiritIds = new List<string>();
            v4.spiritStates = new List<SpiritRuntime.DTO>();
        }

        return v4;
    }

    // ====== Apply ======

    private void Apply(SaveDataV1 s)
    {
        // If the saved scene is different, load it first, then apply once loaded.
        var active = SceneManager.GetActiveScene().name;
        if (!string.Equals(active, s.scene, StringComparison.Ordinal))
        {
            Debug.Log($"[SaveManager] Loading target scene '{s.scene}' (current '{active}') before applying v1 save...");
            SceneManager.sceneLoaded += OnSceneLoadedThenApplyV1;
            SceneManager.LoadScene(s.scene, LoadSceneMode.Single);
            return;

            void OnSceneLoadedThenApplyV1(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnSceneLoadedThenApplyV1;
                ApplyNowV1(s);
                Debug.Log("[SaveManager] Load apply finished after scene switch (v1).");
            }
        }

        ApplyNowV1(s);
    }

    private void Apply(SaveDataV2 s)
    {
        var active = SceneManager.GetActiveScene().name;
        if (!string.Equals(active, s.scene, StringComparison.Ordinal))
        {
            Debug.Log($"[SaveManager] Loading target scene '{s.scene}' (current '{active}') before applying v2 save...");
            SceneManager.sceneLoaded += OnSceneLoadedThenApplyV2;
            SceneManager.LoadScene(s.scene, LoadSceneMode.Single);
            return;

            void OnSceneLoadedThenApplyV2(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnSceneLoadedThenApplyV2;
                ApplyNowV2(s);
                Debug.Log("[SaveManager] Load apply finished after scene switch (v2).");
            }
        }

        ApplyNowV2(s);
    }

    private void Apply(SaveDataV3 s)
    {
        var active = SceneManager.GetActiveScene().name;
        if (!string.Equals(active, s.scene, StringComparison.Ordinal))
        {
            Debug.Log($"[SaveManager] Loading target scene '{s.scene}' (current '{active}') before applying v3 save...");
            SceneManager.sceneLoaded += OnSceneLoadedThenApplyV3;
            SceneManager.LoadScene(s.scene, LoadSceneMode.Single);
            return;

            void OnSceneLoadedThenApplyV3(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnSceneLoadedThenApplyV3;
                ApplyNowV3(s);
                Debug.Log("[SaveManager] Load apply finished after scene switch (v3).");
            }
        }

        ApplyNowV3(s);
    }

    private void Apply(SaveDataV4 s)
    {
        var active = SceneManager.GetActiveScene().name;
        if (!string.Equals(active, s.scene, StringComparison.Ordinal))
        {
            Debug.Log($"[SaveManager] Loading target scene '{s.scene}' (current '{active}') before applying v4 save...");
            SceneManager.sceneLoaded += OnSceneLoadedThenApplyV4;
            SceneManager.LoadScene(s.scene, LoadSceneMode.Single);
            return;

            void OnSceneLoadedThenApplyV4(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= OnSceneLoadedThenApplyV4;
                ApplyNowV4(s);
                Debug.Log("[SaveManager] Load apply finished after scene switch (v4).");
            }
        }

        ApplyNowV4(s);
    }

    private void ApplyNowV1(SaveDataV1 s)
    {
        // Player
        var player = GameObject.FindWithTag("Player");
        if (player)
        {
            player.transform.position = new Vector3(s.playerX, s.playerY, s.playerZ);
            player.transform.rotation = Quaternion.Euler(0f, s.playerYaw, 0f);
        }

        // Time
        var tm = TimeManager.Instance;
        if (tm)
        {
            tm.SetClock(Mathf.Max(1, s.day), Mathf.Clamp(s.hour, 0, 23), Mathf.Clamp(s.minute, 0, 59));
        }

        // v1 had no spirits — ensure manager doesn't keep stale data
        var sm = SpiritManager.Instance;
        if (sm != null)
        {
            sm.ClearOwned(); // old saves = no spirits
        }
    }

    private void ApplyNowV2(SaveDataV2 s)
    {
        // Reuse v1 application for player/time first
        ApplyNowV1(s);

        // Then apply spirits
        var sm = SpiritManager.Instance;
        if (sm != null)
        {
            sm.SetOwnedFromList(s.ownedSpiritIds);
        }
        else
        {
            if (s.ownedSpiritIds != null && s.ownedSpiritIds.Count > 0)
                Debug.LogWarning("[SaveManager] SpiritManager not found while applying ownedSpiritIds. They will be ignored until the manager exists.");
        }
    }

    private void ApplyNowV3(SaveDataV3 s)
    {
        // Reuse v2 application for player/time and owned first
        ApplyNowV2(s);

        // Then apply pending
        var sm = SpiritManager.Instance;
        if (sm != null)
        {
            sm.SetPendingFromList(s.pendingSpiritIds);
        }
        else if (s.pendingSpiritIds != null && s.pendingSpiritIds.Count > 0)
        {
            Debug.LogWarning("[SaveManager] SpiritManager not found while applying pendingSpiritIds. They will be ignored until the manager exists.");
        }
    }

    private void ApplyNowV4(SaveDataV4 s)
    {
        // First, reuse v3 behavior (player/time, owned, pending)
        ApplyNowV3(s);

        // Then apply runtime states for OWNED spirits
        var sm = SpiritManager.Instance;
        if (sm != null)
        {
            sm.ApplyStatesFromDTOs(s.spiritStates);
        }
        else if (s.spiritStates != null && s.spiritStates.Count > 0)
        {
            Debug.LogWarning("[SaveManager] SpiritManager not found while applying v4 spiritStates. They will be ignored until the manager exists.");
        }
    }

    // ====== IO ======

    private async Task SaveAsync()
    {
        try
        {
            var data = CaptureV4(); // always save as v4 going forward
            var json = JsonUtility.ToJson(data, prettyPrint);

            // atomic write
            var tmp = PathFull + ".tmp";
            var bytes = Encoding.UTF8.GetBytes(json);
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                await fs.WriteAsync(bytes, 0, bytes.Length);

            if (File.Exists(PathFull)) File.Delete(PathFull);
            File.Move(tmp, PathFull);
            Debug.Log($"[SaveManager] Saved slot {currentSlot} → {PathFull}\nVersion: {data.version} | Owned: {data.ownedSpiritIds?.Count ?? 0} | Pending: {data.pendingSpiritIds?.Count ?? 0} | States: {data.spiritStates?.Count ?? 0}");
            OnSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Save failed: {e}");
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            Debug.Log($"[SaveManager] Load requested for slot {currentSlot} @ {PathFull}");
            if (!HasSave())
            {
                Debug.Log("[SaveManager] No save file yet.");
                return;
            }

            var bytes = await File.ReadAllBytesAsync(PathFull);
            var json = Encoding.UTF8.GetString(bytes);

            // Probe version
            int version = 1; // assume v1 if not present
            try
            {
                var probe = JsonUtility.FromJson<VersionProbe>(json);
                if (probe.version > 0) version = probe.version;
            }
            catch { /* fall back to v1 */ }

            if (version >= 4)
            {
                var data = JsonUtility.FromJson<SaveDataV4>(json);
                Apply(data);
            }
            else if (version >= 3)
            {
                var data = JsonUtility.FromJson<SaveDataV3>(json);
                Apply(data);
            }
            else if (version >= 2)
            {
                var data = JsonUtility.FromJson<SaveDataV2>(json);
                Apply(data);
            }
            else
            {
                var data = JsonUtility.FromJson<SaveDataV1>(json);
                Apply(data);
            }

            OnLoaded?.Invoke();
            Debug.Log($"[SaveManager] Load complete (slot {currentSlot}, v{version}).");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Load failed: {e}");
        }
    }
}