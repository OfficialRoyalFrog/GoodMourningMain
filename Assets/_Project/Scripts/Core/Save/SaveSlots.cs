using System;
using System.IO;
using System.Security;
using UnityEngine;

/// <summary>
/// Centralizes slot file names and lightweight save summaries for menus.
/// </summary>
public static class SaveSlots
{
    public const int MaxSlots = 3;

    /// <summary>
    /// File pattern: save_slot{n}.json  (n = 1..3)
    /// </summary>
    public static string GetPath(int slot)
    {
        slot = Mathf.Clamp(slot, 1, MaxSlots);
        string fileName = $"save_slot{slot}.json";
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    /// <summary>
    /// Returns true if the slot file exists.
    /// </summary>
    public static bool Exists(int slot) => File.Exists(GetPath(slot));

    /// <summary>
    /// Minimal data we show in the menu without fully loading the world.
    /// </summary>
    [Serializable]
    public struct SaveSummary
    {
        public bool exists;
        public int version;
        public string scene;        // non-nullable; we default to empty
        public long savedUtcTicks;
        public int day, hour, minute;
    }

    /// <summary>
    /// Try to read a <see cref="SaveSummary"/> for the given slot.
    /// Returns false if the file is missing or invalid.
    /// </summary>
    public static bool TryReadSummary(int slot, out SaveSummary summary)
    {
        summary = default;
        string path = GetPath(slot);
        if (!File.Exists(path)) return false;

        try
        {
            // Read JSON (files are written as UTF-8 without BOM)
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return false;

            // Parse using a minimal mirror of the save schema.
            var m = JsonUtility.FromJson<MirrorV1>(json);
            if (m == null) return false;

            summary.exists = true;
            summary.version = m.version;
            summary.scene = string.IsNullOrEmpty(m.scene) ? string.Empty : m.scene;
            summary.savedUtcTicks = m.savedUtcTicks;
            summary.day = m.day;
            summary.hour = m.hour;
            summary.minute = m.minute;
            return true;
        }
        catch (IOException e)
        {
            Debug.LogWarning($"[SaveSlots] IO error reading slot {slot} at '{path}'.");
            Debug.LogException(e);
            return false;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.LogWarning($"[SaveSlots] Unauthorized to read slot {slot} at '{path}'.");
            Debug.LogException(e);
            return false;
        }
        catch (SecurityException e)
        {
            Debug.LogWarning($"[SaveSlots] Security error reading slot {slot} at '{path}'.");
            Debug.LogException(e);
            return false;
        }
        catch (ArgumentException e)
        {
            Debug.LogWarning($"[SaveSlots] Invalid path for slot {slot}: '{path}'.");
            Debug.LogException(e);
            return false;
        }
        catch (NotSupportedException e)
        {
            Debug.LogWarning($"[SaveSlots] Path format not supported for slot {slot}: '{path}'.");
            Debug.LogException(e);
            return false;
        }
    }

    // Minimal mirror of SaveManager.SaveDataV1 for summary purposes only.
    [Serializable]
    private sealed class MirrorV1
    {
        public int version = 0;
        public string scene = "";     // initialize to empty to avoid warnings; JSON may overwrite
        public long savedUtcTicks = 0L;
        public int day = 0, hour = 0, minute = 0;
    }
}