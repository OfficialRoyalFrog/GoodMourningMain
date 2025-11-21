using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Game.Core.TimeSystem;

public class SpiritManager : MonoBehaviour
{
    public static SpiritManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private SpiritDatabase database;

    public IReadOnlyList<string> OwnedSpiritIds => ownedIds;
    public SpiritDatabase Database => database;

    [Header("Spawning")]
    [Tooltip("BoxCollider area where owned spirits can roam in the hub.")]
    [SerializeField] private BoxCollider spawnArea;

    [Tooltip("Minimum distance between spawned spirits when scattering.")]
    [SerializeField] private float minSeparation = 0.8f;

    [Tooltip("If true, a downward raycast snaps each spirit to the ground.")]
    [SerializeField] private bool snapToGround = true;

    [Tooltip("Layers considered 'ground' for raycast snapping.")]
    [SerializeField] private LayerMask groundMask = ~0; // default = everything

    [Tooltip("Extra height added after ground snap to avoid z-fighting/clipping.")]
    [SerializeField] private float spawnYOffset = 0.05f;

    // runtime (spawned owned spirits)
    private readonly List<GameObject> _spawned = new List<GameObject>();
    // Map spawned instance → spirit id so UI/openers can resolve id reliably
    private readonly Dictionary<GameObject, string> _idByInstance = new Dictionary<GameObject, string>();

    private readonly List<string> ownedIds = new List<string>();
    public event Action OnOwnedChanged;

    // ================== 2B: Summoning ==================

    [Header("Summoning")]
    [Tooltip("Where pending spirits appear to await summoning.")]
    [SerializeField] private Transform summoningSpot;

    // pending (runtime-only for now)
    private readonly List<string> _pendingIds = new List<string>();
    private GameObject _pendingInstance;

    // ================== Tuning (M2) ==================
    [Header("Tuning")]
    [SerializeField] private SpiritTuningSO tuning;

    // ================== Actions (M3) ==================
    [Header("Actions")]
    [SerializeField] private SpiritActionSetSO defaultActionSet; // assign ActionSet_Default in Inspector

// ================== Leveling (M4) ==================
[Header("Leveling")]
[SerializeField] private SpiritLevelingSO leveling; // assign SpiritLeveling_Default in Inspector

    // helper
    private static float Clamp01(float v) => (v < 0f) ? 0f : (v > 1f ? 1f : v);

    // ====================================================================

    // ================== Runtime State (M1) ==================
    [Header("Runtime State (per-owned spirit)")]
    // Not serialized: we rebuild from save; UI reads through public API
    private readonly Dictionary<string, SpiritRuntime> stateById = new Dictionary<string, SpiritRuntime>();

    /// <summary>Raised when a batch of state changes has completed (tick, actions, level-ups). UI can Refresh() on this.</summary>
    public event Action OnStatesChanged;
    public event Action<string,int> OnLevelUp; // (id, newLevel)

    /// <summary>Try to get runtime state for a spirit id.</summary>
    public bool TryGetState(string id, out SpiritRuntime state) => stateById.TryGetValue(id, out state);

    /// <summary>Get state if present; otherwise create with defaults and return it.</summary>
    public SpiritRuntime GetOrCreateState(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!stateById.TryGetValue(id, out var s))
        {
            s = new SpiritRuntime(id, DateTime.UtcNow.Ticks);
            stateById[id] = s;
        }
        return s;
    }

    /// <summary>Read-only snapshot for debug/tools; prefer TryGetState for lookups.</summary>
    public IReadOnlyDictionary<string, SpiritRuntime> StateById => stateById;

    // =============== State Serialization Helpers (for Save v4) ===============

    /// <summary>
    /// Capture DTOs for all currently OWNED spirits only (order stable by id).
    /// </summary>
    public List<SpiritRuntime.DTO> CaptureStatesForOwned()
    {
        var list = new List<SpiritRuntime.DTO>();
        for (int i = 0; i < ownedIds.Count; i++)
        {
            var id = ownedIds[i];
            var s = GetOrCreateState(id);
            list.Add(s.ToDTO());
        }
        return list;
    }

    /// <summary>
    /// Apply a DTO list to our runtime states. We will:
    /// - Ensure a state exists for each OWNED id (create defaults if missing)
    /// - If a DTO exists for that id, overwrite that state's fields
    /// - Ignore DTOs for non-owned ids (owned is source of truth)
    /// Finally, fire OnStatesChanged so UI can refresh.
    /// </summary>
    public void ApplyStatesFromDTOs(List<SpiritRuntime.DTO> dtos)
    {
        // Build a map for quick lookups
        var map = new Dictionary<string, SpiritRuntime.DTO>();
        if (dtos != null)
        {
            for (int i = 0; i < dtos.Count; i++)
            {
                var d = dtos[i];
                if (!string.IsNullOrEmpty(d.id))
                    map[d.id] = d; // last wins
            }
        }

        // For each OWNED id, ensure state exists and then apply if DTO found
        for (int i = 0; i < ownedIds.Count; i++)
        {
            var id = ownedIds[i];
            var s = GetOrCreateState(id);
            if (map.TryGetValue(id, out var d))
            {
                // Clamp & assign through SpiritRuntime.FromDTO for safety
                var rebuilt = SpiritRuntime.FromDTO(d);
                // Copy fields (keep same object reference)
                s.level       = rebuilt.level;
                s.xp01        = rebuilt.xp01;
                s.serenity01  = rebuilt.serenity01;
                s.appetite01  = rebuilt.appetite01;
                s.integrity01 = rebuilt.integrity01;
                s.daysOwned   = rebuilt.daysOwned;
                s.acquiredUtcTicks = rebuilt.acquiredUtcTicks;
            }
            else
            {
                // No DTO? Keep/create defaults; nothing to do
            }
        }

        // Prune states for non-owned ids to keep tidy (consistent with M1.1)
        var keys = stateById.Keys.ToList();
        foreach (var k in keys)
        {
            if (!Has(k)) stateById.Remove(k);
        }

        OnStatesChanged?.Invoke();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        OnOwnedChanged += RespawnAllOwned;
    }

    public bool Has(string spiritId) => !string.IsNullOrEmpty(spiritId) && ownedIds.Contains(spiritId);

    public bool AddOwned(string spiritId)
    {
        if (string.IsNullOrEmpty(spiritId)) return false;
        if (ownedIds.Contains(spiritId)) return false;

        ownedIds.Add(spiritId);

        // Ensure runtime state exists for this spirit
        GetOrCreateState(spiritId);

        OnOwnedChanged?.Invoke();
        // State did change (a new entry was created) — fire the state event too so UI can react later.
        OnStatesChanged?.Invoke();
        return true;
    }

    public bool RemoveOwned(string spiritId)
    {
        if (string.IsNullOrEmpty(spiritId)) return false;
        bool removed = ownedIds.Remove(spiritId);
        if (removed) OnOwnedChanged?.Invoke();
        return removed;
    }

    public void ClearOwned()
    {
        if (ownedIds.Count == 0) return;
        ownedIds.Clear();

        // Optional: prune states that no longer belong to any owned spirit.
        var keys = stateById.Keys.ToList();
        foreach (var k in keys)
        {
            if (!Has(k)) stateById.Remove(k);
        }

        OnOwnedChanged?.Invoke();
        OnStatesChanged?.Invoke();
    }

    public void SetOwnedFromList(IEnumerable<string> ids)
    {
        ownedIds.Clear();
        if (ids != null) ownedIds.AddRange(ids);

        // Ensure state exists for each owned id
        foreach (var id in ownedIds)
            GetOrCreateState(id);

        // Prune states for non-owned ids
        var keys = stateById.Keys.ToList();
        foreach (var k in keys)
        {
            if (!Has(k)) stateById.Remove(k);
        }

        OnOwnedChanged?.Invoke();
        OnStatesChanged?.Invoke();
    }

    Bounds GetSpawnBounds()
    {
        if (spawnArea != null)
        {
            var b = spawnArea.bounds;
            var center = new Vector3(b.center.x, spawnArea.transform.position.y, b.center.z);
            var size = new Vector3(b.size.x, 0.1f, b.size.z);
            return new Bounds(center, size);
        }
        return new Bounds(Vector3.zero, new Vector3(8f, 0.1f, 6f));
    }

    void DespawnAll()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            var go = _spawned[i];
            if (go)
            {
                _idByInstance.Remove(go); // remove instance→id mapping
                Destroy(go);
            }
        }
        _spawned.Clear();
        _idByInstance.Clear();
    }

    public void RespawnAllOwned()
    {
        DespawnAll();
        if (database == null) return;

        var b = GetSpawnBounds();
        var used = new List<Vector3>();
        foreach (var id in ownedIds)
        {
            var so = database.Get(id);
            if (so == null || so.HubPrefab == null) continue;

            Vector3 posXZ = FindFreeSpot(b, used, minSeparation, 12);
            float y = ResolveSpawnY(posXZ, b);
            Vector3 pos = new Vector3(posXZ.x, y, posXZ.z);
            used.Add(pos);

            var go = Instantiate(so.HubPrefab, pos, Quaternion.identity);
            _spawned.Add(go);
            _idByInstance[go] = id; // record instance→id for UI/openers

            // Apply SO-driven visuals and ensure minimal components exist
            var appear = go.GetComponent<SpiritAppearance>() ?? go.AddComponent<SpiritAppearance>();
            appear.Apply(so);
            EnsureMinimalSpiritComponents(go, id);

            var agent = go.GetComponent<SpiritAgent>();
            if (!agent) agent = go.AddComponent<SpiritAgent>();
            agent.SetRoamBounds(b);

            // Tag the spawned instance with its Spirit Id for later UI/openers
            var idHandle = go.GetComponent<SpiritIdHandle>() ?? go.AddComponent<SpiritIdHandle>();
            AssignSpiritId(idHandle, id);

        }
    }

    /// <summary>
    /// Ensures a spawned spirit instance has the minimum components needed for interaction
    /// and opens the radial on Interact. Safe to call multiple times.
    /// </summary>
    private void EnsureMinimalSpiritComponents(GameObject go, string id)
    {
        if (go == null) return;

        // Collider (3D): use SphereCollider trigger if none exists
        var col = go.GetComponent<Collider>();
        if (!col)
        {
            var sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.35f;
            col = sc;
        }

        // InteractableBase (instant)
        var ia = go.GetComponent<InteractableBase>() ?? go.AddComponent<InteractableBase>();
        ia.prompt = string.IsNullOrEmpty(ia.prompt) ? "Interact" : ia.prompt;
        ia.requiresHold = false;

        // SpiritIdHandle tag (backfill if missing)
        var idHandle = go.GetComponent<SpiritIdHandle>() ?? go.AddComponent<SpiritIdHandle>();
        AssignSpiritId(idHandle, id);

        // Radial opener to show the UI when interacted
        var opener = go.GetComponent<SpiritInteractOpener>() ?? go.AddComponent<SpiritInteractOpener>();

        // Ensure opener is in the actionBehaviours list so InteractableBase executes it
        if (ia.actionBehaviours == null)
        {
            ia.actionBehaviours = new MonoBehaviour[] { opener };
        }
        else
        {
            bool has = false;
            for (int i = 0; i < ia.actionBehaviours.Length; i++)
                if (ia.actionBehaviours[i] == opener) { has = true; break; }
            if (!has)
            {
                var old = ia.actionBehaviours;
                var expanded = new MonoBehaviour[old.Length + 1];
                for (int i = 0; i < old.Length; i++) expanded[i] = old[i];
                expanded[old.Length] = opener;
                ia.actionBehaviours = expanded;
            }
        }
    }

    /// <summary>Resolve the owned spirit id for a spawned instance.</summary>
    public bool TryGetIdForInstance(GameObject go, out string id)
    {
        id = null; // ensure out param is always assigned
        if (go == null) return false;
        return _idByInstance.TryGetValue(go, out id);
    }

    Vector3 FindFreeSpot(Bounds b, List<Vector3> used, float minDist, int maxTries)
    {
        for (int t = 0; t < maxTries; t++)
        {
            float x = UnityEngine.Random.Range(b.min.x, b.max.x);
            float z = UnityEngine.Random.Range(b.min.z, b.max.z);
            var p = new Vector3(x, b.center.y, z);

            bool ok = true;
            for (int i = 0; i < used.Count; i++)
            {
                if ((used[i] - p).sqrMagnitude < (minDist * minDist)) { ok = false; break; }
            }
            if (ok) return p;
        }
        return b.center;
    }

    float ResolveSpawnY(Vector3 atXZ, Bounds areaBounds)
    {
        if (snapToGround)
        {
            Vector3 start = atXZ + Vector3.up * 5f;
            if (Physics.Raycast(start, Vector3.down, out var hit, 20f, groundMask, QueryTriggerInteraction.Ignore))
                return hit.point.y + spawnYOffset;
        }
        return areaBounds.center.y + spawnYOffset;
    }

    private void OnEnable()
    {
        RespawnAllOwned();
        TryShowPending();

        // === Subscribe to time events (M2) ===
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnHourChanged  += OnHourChanged;
            TimeManager.Instance.OnDayStarted   += OnDayStarted;
        }
    }

    private void OnDisable()
    {
        // === Unsubscribe (paired with OnEnable) ===
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnHourChanged  -= OnHourChanged;
            TimeManager.Instance.OnDayStarted   -= OnDayStarted;
        }
    }

    private void OnDestroy()
    {
        OnOwnedChanged -= RespawnAllOwned;
    }

    // ================== Ticks (M2) ==================
    private void OnHourChanged(int hour)
    {
        ApplyHourly(hour);
    }

    private void OnDayStarted(int dayIndex)
    {
        ApplyDaily(dayIndex);
    }



    /// <summary>
    /// Called when TimeManager fires an hour change. Applies meter updates to ALL owned spirits, then fires OnStatesChanged once.
    /// </summary>
    private void ApplyHourly(int currentHour)
    {
        if (tuning == null) return;               // nothing to do without tuning
        if (ownedIds == null || ownedIds.Count == 0) return;

        bool isNight = IsNightNow();



        // Batch update
        for (int i = 0; i < ownedIds.Count; i++)
        {
            string id = ownedIds[i];
            var s = GetOrCreateState(id);
            if (s == null) continue;

            // Appetite decays
            s.appetite01 -= tuning.appetiteDecayPerHour;

            // Serenity regenerates (more at night)
            float serenityDelta = tuning.serenityRegenPerHour * (isNight ? tuning.nightSerenityMultiplier : 1f);
            s.serenity01 += serenityDelta;
            s.integrity01 += s.serenity01 * tuning.integrityRegenK - (1f - s.appetite01) * tuning.appetitePenaltyK;

            // Clamp all meters if enabled
            if (tuning.clamp01)
            {
                s.appetite01 = Clamp01(s.appetite01);
                s.serenity01 = Clamp01(s.serenity01);
                s.integrity01 = Clamp01(s.integrity01);
            }
        }

        // === Complete any assignments that are due ===
        float nowH = GetCurrentGameHours();
        for (int i = 0; i < ownedIds.Count; i++)
        {
            var id = ownedIds[i];
            var s = GetOrCreateState(id);
            if (s == null || s.activeAssignments.Count == 0) continue;

            for (int j = s.activeAssignments.Count - 1; j >= 0; j--)
            {
                var a = s.activeAssignments[j];
                if (nowH >= a.completeAtGameHour)
                {
                    // Resolve action definition to apply completion effects
                    SpiritActionDefSO def = null;
                    if (defaultActionSet != null && defaultActionSet.Actions != null)
                        def = defaultActionSet.Actions.FirstOrDefault(x => x != null && x.Id == a.actionId);

                    if (def != null)
                    {
                        if (def.DeltaSerenity != 0f) s.serenity01 = Clamp01(s.serenity01 + def.DeltaSerenity);
                        if (def.DeltaAppetite != 0f) s.appetite01 = Clamp01(s.appetite01 + def.DeltaAppetite);
                        if (def.DeltaIntegrity != 0f) s.integrity01 = Clamp01(s.integrity01 + def.DeltaIntegrity);
                        if (def.XpGain > 0f) TryAddXp(id, Mathf.Clamp01(def.XpGain));
                    }

                    // Remove completed assignment
                    s.activeAssignments.RemoveAt(j);
                }
            }
        }

        // Single event after batch
        OnStatesChanged?.Invoke();
    }

    /// <summary>
    /// Called when a new day starts. Increments daysOwned for ALL owned spirits, then fires OnStatesChanged.
    /// </summary>
    private void ApplyDaily(int dayIndex)
    {
        if (ownedIds == null || ownedIds.Count == 0) return;

        for (int i = 0; i < ownedIds.Count; i++)
        {
            var s = GetOrCreateState(ownedIds[i]);
            if (s == null) continue;
            s.daysOwned = Mathf.Max(0, s.daysOwned + 1);
        }

        OnStatesChanged?.Invoke();
    }

    /// <summary>Night check using TimeManager windows (SunsetHour->SunriseHour, wraps midnight).</summary>
    private bool IsNightNow()
    {
        var tm = TimeManager.Instance;
        if (tm == null) return false;

        float now = tm.CurrentHourFloat;
        float start = tm.SunsetHour;
        float end = tm.SunriseHour;

        // Interval may wrap midnight: consider inside if between start..24 OR 0..end
        if (start <= end)
            return now >= start && now < end;       // non-wrapping (rare)
        else
            return (now >= start) || (now < end);   // wrapping over midnight (typical)
    }

private void ApplyLevelRewards(SpiritRuntime s)
{
    if (s == null || leveling == null) return;
    // Optional small per-level bonuses; safe if zero
    s.serenity01  = Clamp01(s.serenity01  + leveling.serenityRegenBonusPerLevel);
    s.appetite01  = Clamp01(s.appetite01  + leveling.appetiteDecayBonusPerLevel);
    // (Integrity or other rewards can be added to SpiritLevelingSO later if desired)
}

    // ===== Helpers for Actions (M3) =====
    private float GetCurrentGameHours()
    {
        var tm = TimeManager.Instance;
        if (tm == null) return 0f;
        return tm.DayIndex * 24f + tm.Hour + (tm.Minute / 60f);
    }

    /// <summary>Add fractional XP (0..1) to the spirit's current level progress. Leveling curve arrives in M4.</summary>
public void TryAddXp(string spiritId, float deltaXp01)
{
    if (deltaXp01 <= 0f) return;
    if (!TryGetState(spiritId, out var s) || s == null) return;

    // Add normalized XP toward the current level
    s.xp01 = Mathf.Clamp01(s.xp01 + deltaXp01);

    int cap = (leveling != null && leveling.levelCap > 0) ? leveling.levelCap : 99;

    // Handle multiple potential level-ups in one grant
    while (s.xp01 >= 1f && s.level < cap)
    {
        s.level = Mathf.Max(1, s.level + 1);
        s.xp01 -= 1f; // keep overflow toward next level
        ApplyLevelRewards(s);
        OnLevelUp?.Invoke(spiritId, s.level);
    }

    // Clamp and notify
    s.ClampAll();
    OnStatesChanged?.Invoke();
}

    /// <summary>
    /// Execute a data-defined action on a spirit (cooldowns, optional item cost, instant effects or assignment stub).
    /// Returns true on success; false with a reason.
    /// </summary>
    public bool TryExecuteAction(string spiritId, SpiritActionDefSO def, out string reason)
    {
        reason = null;
        if (def == null) { reason = "No action."; return false; }
        if (!Has(spiritId)) { reason = "Spirit not owned."; return false; }
        if (!TryGetState(spiritId, out var s) || s == null) { reason = "No state."; return false; }
        if (def.Disabled) { reason = "Action disabled."; return false; }

        // Cooldown check in in-game hours
        float nowH = GetCurrentGameHours();
        if (s.cooldownByAction.TryGetValue(def.Id, out var readyH))
        {
            if (nowH < readyH)
            {
                reason = $"On cooldown ({readyH - nowH:0.0}h).";
                return false;
            }
        }

        // Optional inventory cost
        if (def.ConsumesItem)
        {
            if (def.RequiredItem == null || def.RequiredItemCount < 1)
            {
                reason = "Invalid item requirement.";
                return false;
            }
            if (Inventory.Instance == null)
            {
                reason = "No Inventory.";
                return false;
            }
            if (!Inventory.Instance.TryConsume(def.RequiredItem, def.RequiredItemCount))
            {
                reason = $"Need {def.RequiredItemCount} × {def.RequiredItem.DisplayName}.";
                return false;
            }
        }

        bool isAssignment = def.AssignmentDurationHours > 0f;

        if (!isAssignment)
        {
            // Apply instant effects
            if (def.DeltaSerenity != 0f)  s.serenity01  = Mathf.Clamp01(s.serenity01  + def.DeltaSerenity);
            if (def.DeltaAppetite != 0f)  s.appetite01  = Mathf.Clamp01(s.appetite01  + def.DeltaAppetite);
            if (def.DeltaIntegrity != 0f) s.integrity01 = Mathf.Clamp01(s.integrity01 + def.DeltaIntegrity);

            if (def.XpGain > 0f)
                TryAddXp(spiritId, Mathf.Clamp01(def.XpGain)); // placeholder: xp01 fraction; curve-based in M4

            if (def.CooldownHours > 0f)
                s.cooldownByAction[def.Id] = nowH + def.CooldownHours;

            OnStatesChanged?.Invoke();
            return true;
        }
else
{
    // Enqueue assignment to complete later; effects applied on completion.
    float completeAt = nowH + def.AssignmentDurationHours;
    s.activeAssignments.Add(new SpiritRuntime.AssignmentEntry
    {
        actionId = def.Id,
        completeAtGameHour = completeAt
    });

    // Optional: set cooldown now so it can’t be immediately queued again
    if (def.CooldownHours > 0f)
        s.cooldownByAction[def.Id] = nowH + def.CooldownHours;

    OnStatesChanged?.Invoke();
    return true;
}
    }

    // ================== 2B: Pending / Summoning ==================

    public bool HasPending => _pendingIds.Count > 0;

    /// <summary>Read-only view of queued pending spirit IDs (for save capture).</summary>
    public IReadOnlyList<string> PendingIds => _pendingIds;

    /// <summary>Queue a spirit to appear at the summoning spot and await summoning.</summary>
    public void QueuePending(string spiritId)
    {
        if (string.IsNullOrEmpty(spiritId)) return;
        if (_pendingIds.Contains(spiritId)) return;
        _pendingIds.Add(spiritId);
        TryShowPending();
    }

    /// <summary>Spawn the first pending spirit (if any) at the summoning spot with a Summon interaction.</summary>
private void TryShowPending()
{
    if (_pendingInstance != null) return;
    if (_pendingIds.Count == 0) return;
    if (database == null || summoningSpot == null) return;

    var so = database.Get(_pendingIds[0]);
    if (so == null || so.HubPrefab == null) return;

    // --- Ground-snapped position at the summoning spot using existing ResolveSpawnY() ---
    var bounds = GetSpawnBounds();
    Vector3 spotXZ = new Vector3(summoningSpot.position.x, 0f, summoningSpot.position.z);
    float y = ResolveSpawnY(spotXZ, bounds);
    Vector3 pos = new Vector3(spotXZ.x, y, spotXZ.z);

    // Spawn pending instance at a sane rotation (upright)
    _pendingInstance = Instantiate(so.HubPrefab, pos, Quaternion.identity);
    _pendingInstance.transform.rotation = Quaternion.identity;

    // Apply spirit-specific visuals so pending copy shows correct sprite/colors
    var appearance = _pendingInstance.GetComponent<SpiritAppearance>() ?? _pendingInstance.AddComponent<SpiritAppearance>();
    appearance.Apply(so);

    // Ensure a SpiritAgent exists, but keep it stationary (hover only)
    var agent = _pendingInstance.GetComponent<SpiritAgent>() ?? _pendingInstance.AddComponent<SpiritAgent>();
        agent.moveSpeed = 0f; // no wandering here
        agent.turnSpeed = 0f; // stop steering rotation while pending
    

    // Ensure there's a collider for interaction
    var col = _pendingInstance.GetComponent<Collider>();
    if (!col)
    {
        var sc = _pendingInstance.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.35f;
    }

    // Make it interactable via a hold: "Summon"
    var ia = _pendingInstance.GetComponent<InteractableBase>() ?? _pendingInstance.AddComponent<InteractableBase>();
    ia.prompt = "Summon";
    ia.requiresHold = true;
    ia.holdSecondsOverride = 0.8f;
    ia.priority = 5;

    // Add the action and (IMPORTANT) assign it to actionBehaviours so InteractableBase can run it safely
    var action = _pendingInstance.GetComponent<SpiritSummonAction>() ?? _pendingInstance.AddComponent<SpiritSummonAction>();
    action.Manager = this;

    // Prevent NullReference inside InteractableBase.Interact by ensuring this array is non-null
    ia.actionBehaviours = new MonoBehaviour[] { action };
}

    /// <summary>Replace the current pending queue and refresh the pending visual.</summary>
    public void SetPendingFromList(IEnumerable<string> ids)
    {
        _pendingIds.Clear();
        if (ids != null)
            _pendingIds.AddRange(ids);

        // Reset any existing pending instance and show the first item, if any
        ClearPendingVisual();
        TryShowPending();
    }

    private void ClearPendingVisual()
    {
        if (_pendingInstance)
        {
            Destroy(_pendingInstance);
            _pendingInstance = null;
        }
    }

    /// <summary>Complete summoning for the first pending spirit: moves to owned and respawns in roam area.</summary>
    public void CompleteSummon()
    {
        if (_pendingIds.Count == 0) return;
        string id = _pendingIds[0];
        _pendingIds.RemoveAt(0);

        AddOwned(id);
        ClearPendingVisual();
        TryShowPending();

        var so = database?.Get(id);
        if (so != null)
            Debug.Log($"[Spirits] Summoned: {so.DisplayName}");
    }

    #if UNITY_EDITOR

    [ContextMenu("DEBUG/Execute First Action On First Spirit")]
    private void DEBUG_ExecuteFirstActionOnFirstSpirit()
    {
        if (OwnedSpiritIds.Count == 0) { Debug.Log("[Actions] No owned spirits."); return; }
        if (defaultActionSet == null || defaultActionSet.Actions == null || defaultActionSet.Actions.Count == 0)
        {
            Debug.Log("[Actions] No actions in defaultActionSet.");
            return;
        }
        string id = OwnedSpiritIds[0];
        var def = defaultActionSet.Actions[0];
        if (def == null) { Debug.Log("[Actions] First action is null."); return; }

        if (TryExecuteAction(id, def, out var reason))
            Debug.Log($"[Actions] {def.DisplayName} executed on {id}.");
        else
            Debug.Log($"[Actions] Failed: {reason}");
    }
    [ContextMenu("DEBUG/Randomize First Owned State")]
    private void DEBUG_RandomizeFirstOwnedState()
    {
        if (ownedIds.Count == 0) { Debug.Log("[SpiritManager] No owned spirits to randomize."); return; }
        var id = ownedIds[0];
        var s = GetOrCreateState(id);
        var rng = new System.Random();
        s.level       = Mathf.Max(1, s.level + rng.Next(-1, 2)); // -1, 0, +1
        s.xp01        = Mathf.Clamp01((float)rng.NextDouble());
        s.serenity01  = Mathf.Clamp01((float)rng.NextDouble());
        s.appetite01  = Mathf.Clamp01((float)rng.NextDouble());
        s.integrity01 = Mathf.Clamp01((float)rng.NextDouble());
        s.daysOwned   = Mathf.Max(0, s.daysOwned + rng.Next(0, 3)); // +0..+2
        Debug.Log($"[SpiritManager] Randomized state for {id}.");
        OnStatesChanged?.Invoke();
    }

    [ContextMenu("DEBUG/Log First Owned State")]
    private void DEBUG_LogFirstOwnedState()
    {
        if (ownedIds.Count == 0) { Debug.Log("[SpiritManager] No owned spirits."); return; }
        var id = ownedIds[0];
        if (TryGetState(id, out var s))
            Debug.Log($"[SpiritManager] State for {id}: L{ s.level } xp={s.xp01:0.00} ser={s.serenity01:0.00} app={s.appetite01:0.00} int={s.integrity01:0.00} days={s.daysOwned} acquired={new DateTime(s.acquiredUtcTicks, DateTimeKind.Utc):u}");
        else
            Debug.Log("[SpiritManager] No state found for first owned id?");
    }

    [ContextMenu("DEBUG/Add Spirit A (spirit_a)")]
    private void DEBUG_AddA()
    {
        AddOwned("spirit_a");
        Debug.Log("[SpiritManager] Added 'spirit_a'");
    }

    [ContextMenu("DEBUG/Add Spirit B (spirit_b)")]
    private void DEBUG_AddB()
    {
        AddOwned("spirit_b");
        Debug.Log("[SpiritManager] Added 'spirit_b'");
    }

    [ContextMenu("DEBUG/Add Spirit C (spirit_c)")]
    private void DEBUG_AddC()
    {
        AddOwned("spirit_c");
        Debug.Log("[SpiritManager] Added 'spirit_c'");
    }

    [ContextMenu("DEBUG/Clear All Spirits")]
    private void DEBUG_Clear()
    {
        ClearOwned();
        Debug.Log("[SpiritManager] Cleared all owned spirits");
    }

    [ContextMenu("DEBUG/Queue Pending A")]
    private void DEBUG_QueueA() { QueuePending("spirit_a"); }

    [ContextMenu("DEBUG/Queue Pending B")]
    private void DEBUG_QueueB() { QueuePending("spirit_b"); }

    [ContextMenu("DEBUG/Queue Pending C")]
    private void DEBUG_QueueC() { QueuePending("spirit_c"); }
    #endif

    private static void AssignSpiritId(SpiritIdHandle handle, string id)
    {
        if (handle == null || string.IsNullOrEmpty(id)) return;
        var f = typeof(SpiritIdHandle).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f != null) f.SetValue(handle, id);
    }
}
