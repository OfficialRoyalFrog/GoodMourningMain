using UnityEngine;

[CreateAssetMenu(menuName = "Game/Spirits/Spirit Leveling", fileName = "SpiritLeveling")]
public class SpiritLevelingSO : ScriptableObject
{
    [Header("Leveling Curve")]
    [Tooltip("XP required to reach each level (index 0 unused).")]
    public AnimationCurve xpToLevelCurve = AnimationCurve.Linear(1, 1, 10, 10);
    [Tooltip("If true, uses xpTable instead of curve.")]
    public bool useTable = false;
    public int[] xpTable = { 0, 1, 3, 6, 10 }; // example thresholds
    public int levelCap = 10;

    [Header("Per-Level Rewards (Optional)")]
    public float serenityRegenBonusPerLevel = 0.05f;
    public float appetiteDecayBonusPerLevel = -0.02f;
}