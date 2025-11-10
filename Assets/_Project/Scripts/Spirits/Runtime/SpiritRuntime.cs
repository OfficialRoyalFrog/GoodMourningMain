using System;
using System.Collections.Generic;
using UnityEngine;

/// Plain data container for per-spirit runtime state (NOT a MonoBehaviour).
/// Default values are "sane middles" so nothing looks broken at first open.
[Serializable]
public class SpiritRuntime
{
    public string id;
    public int    level        = 1;     // ≥ 1
    public float  xp01         = 0f;    // 0..1 progress within current level
    public float  serenity01   = 0.5f;  // 0..1
    public float  appetite01   = 1.0f;  // start full
    public float  integrity01  = 1.0f;  // 0..1 (full = healthy)
    public int    daysOwned    = 0;
    public long   acquiredUtcTicks;     // when ownership began (UTC)

    // Per-spirit tuning multipliers (persisted)
    public float  multSerenityRegen     = 1.0f;
    public float  multAppetiteDecay     = 1.0f;
    public float  multIntegrityRegenK   = 1.0f;
    public float  multAppetitePenaltyK  = 1.0f;

    // --- Cooldowns (per action), stored in GAME HOURS (Day*24 + Hour + Minute/60) ---
    [Serializable]
    public struct CooldownEntry { public string actionId; public float nextAllowedGameHour; }

    // Runtime map (not serialized directly; persisted via DTO.cooldowns)
    public readonly Dictionary<string, float> cooldownByAction = new Dictionary<string, float>();

    // --- Active assignments (jobs) that complete in the future, stored in GAME HOURS ---
    [Serializable]
    public struct AssignmentEntry { public string actionId; public float completeAtGameHour; }

    // Runtime list; persisted via DTO.assignments
    public readonly List<AssignmentEntry> activeAssignments = new List<AssignmentEntry>();

    public SpiritRuntime() { }

    public SpiritRuntime(string spiritId, long utcNowTicks)
    {
        id = spiritId;
        acquiredUtcTicks = utcNowTicks;
    }

    /// <summary>Clamp all meter fields into [0,1] and enforce level/days minimums.</summary>
    public void ClampAll()
    {
        level       = Mathf.Max(1, level);
        xp01        = Mathf.Clamp01(xp01);
        serenity01  = Mathf.Clamp01(serenity01);
        appetite01  = Mathf.Clamp01(appetite01);
        integrity01 = Mathf.Clamp01(integrity01);
        daysOwned   = Mathf.Max(0, daysOwned);
        if (string.IsNullOrEmpty(id)) Debug.LogWarning("[SpiritRuntime] Missing spirit id; should be assigned by SpiritManager.");
    }

    /// <summary>Ensure required fields are initialized (useful post-migration).</summary>
    public void EnsureDefaults(string spiritId, long utcNowTicks)
    {
        if (string.IsNullOrEmpty(id)) id = spiritId;
        if (level < 1) level = 1;
        if (acquiredUtcTicks == 0L) acquiredUtcTicks = utcNowTicks;
        if (multSerenityRegen <= 0f)    multSerenityRegen = 1f;
        if (multAppetiteDecay <= 0f)    multAppetiteDecay = 1f;
        if (multIntegrityRegenK <= 0f)  multIntegrityRegenK = 1f;
        if (multAppetitePenaltyK <= 0f) multAppetitePenaltyK = 1f;
        ClampAll();
    }

    /// <summary>
    /// Utility for cooldowns: returns true if the action is currently on cooldown given the current game hour.
    /// </summary>
    public bool IsOnCooldown(string actionId, float currentGameHour)
    {
        if (string.IsNullOrEmpty(actionId)) return false;
        if (!cooldownByAction.TryGetValue(actionId, out var next)) return false;
        return currentGameHour < next;
    }

    // --- DTO used by Save v4 (added now so v4 is trivial to plug later) ---
    [Serializable]
    public struct DTO
    {
        public string id;
        public int    level;
        public float  xp01;
        public float  serenity01;
        public float  appetite01;
        public float  integrity01;
        public int    daysOwned;
        public long   acquiredUtcTicks;
        public float  multSerenityRegen;
        public float  multAppetiteDecay;
        public float  multIntegrityRegenK;
        public float  multAppetitePenaltyK;
        public CooldownEntry[] cooldowns;
        public AssignmentEntry[] assignments;
    }

    public DTO ToDTO()
    {
        var list = new List<CooldownEntry>();
        foreach (var kv in cooldownByAction)
            list.Add(new CooldownEntry { actionId = kv.Key, nextAllowedGameHour = kv.Value });

        var assign = activeAssignments != null ? activeAssignments.ToArray() : Array.Empty<AssignmentEntry>();

        return new DTO
        {
            id = id,
            level = level,
            xp01 = xp01,
            serenity01 = serenity01,
            appetite01 = appetite01,
            integrity01 = integrity01,
            daysOwned = daysOwned,
            acquiredUtcTicks = acquiredUtcTicks,
            multSerenityRegen = multSerenityRegen,
            multAppetiteDecay = multAppetiteDecay,
            multIntegrityRegenK = multIntegrityRegenK,
            multAppetitePenaltyK = multAppetitePenaltyK,
            cooldowns = list.ToArray(),
            assignments = assign
        };
    }

    public static SpiritRuntime FromDTO(DTO d)
    {
        var r = new SpiritRuntime
        {
            id = d.id,
            level = Mathf.Max(1, d.level),
            xp01 = Mathf.Clamp01(d.xp01),
            serenity01 = Mathf.Clamp01(d.serenity01),
            appetite01 = Mathf.Clamp01(d.appetite01),
            integrity01 = Mathf.Clamp01(d.integrity01),
            daysOwned = Mathf.Max(0, d.daysOwned),
            acquiredUtcTicks = d.acquiredUtcTicks,
            multSerenityRegen = (d.multSerenityRegen > 0f) ? d.multSerenityRegen : 1f,
            multAppetiteDecay = (d.multAppetiteDecay > 0f) ? d.multAppetiteDecay : 1f,
            multIntegrityRegenK = (d.multIntegrityRegenK > 0f) ? d.multIntegrityRegenK : 1f,
            multAppetitePenaltyK = (d.multAppetitePenaltyK > 0f) ? d.multAppetitePenaltyK : 1f
        };

        // Restore cooldowns map
        if (d.cooldowns != null)
        {
            r.cooldownByAction.Clear();
            for (int i = 0; i < d.cooldowns.Length; i++)
            {
                var e = d.cooldowns[i];
                if (!string.IsNullOrEmpty(e.actionId))
                    r.cooldownByAction[e.actionId] = e.nextAllowedGameHour;
            }
        }

        // Restore assignments list
        if (d.assignments != null)
        {
            r.activeAssignments.Clear();
            r.activeAssignments.AddRange(d.assignments);
        }

        // Final safety: clamp and ensure sane defaults
        r.ClampAll();
        if (string.IsNullOrEmpty(r.id)) r.id = d.id;

        return r;
    }
}