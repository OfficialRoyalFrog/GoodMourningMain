using UnityEngine;

[CreateAssetMenu(menuName = "Game/Spirits/Spirit Tuning", fileName = "SpiritTuning")]
public class SpiritTuningSO : ScriptableObject
{
    [Header("Hourly Changes (0..0.10 typical)")]
    [Tooltip("How much appetite falls per in-game hour. 0.05 = -5%/hr.")]
    [Range(0f, 0.10f)] public float appetiteDecayPerHour = 0.03f;

    [Tooltip("How much serenity regenerates per in-game hour during day.")]
    [Range(0f, 0.10f)] public float serenityRegenPerHour = 0.02f;

    [Tooltip("Multiplier applied at night to serenity regen (e.g., 1.5x).")]
    [Range(0f, 3f)] public float nightSerenityMultiplier = 1.5f;

    [Header("Integrity Dynamics")]
    [Tooltip("Baseline integrity gain per hour from current serenity (K).")]
    [Range(0f, 0.50f)] public float integrityRegenK = 0.10f;

    [Tooltip("Penalty to integrity per hour from hunger (1 - appetite) (K).")]
    [Range(0f, 0.50f)] public float appetitePenaltyK = 0.08f;

    [Header("Clamping/Smoothing")]
    [Tooltip("Clamp all meters strictly to [0..1]. Always enabled.")]
    public bool clamp01 = true;

    [Tooltip("Optional visual smoothing is handled by UI. Keep logic discrete.")]
    public bool enableSmoothingWindow = false;

    [Tooltip("Window length (hours) if a smoothing filter is later added.")]
    [Range(0.1f, 6f)] public float smoothingWindowHours = 1f;
}