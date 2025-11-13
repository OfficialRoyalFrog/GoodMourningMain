using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CultStatsController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private SpiritManager spiritManager;

    [Header("Left Counters (TMP Values)")]
    [SerializeField] private TMP_Text valueSpirits;
    [SerializeField] private TMP_Text valuePassedOver;
    [SerializeField] private TMP_Text valueGraves;

    [Header("Right Meters (Filled Images only)")]
    [SerializeField] private Image serenityFill;

    [SerializeField] private Image appetiteFill;

    [SerializeField] private Image integrityFill;

    [Header("Placeholders (Inspector-editable for now)")]
    [SerializeField, Min(0)] private int passedOver = 0;
    [SerializeField, Min(0)] private int graves = 0;
    [Range(0f,1f)] public float serenity01  = 0.75f;
    [Range(0f,1f)] public float appetite01  = 0.55f;
    [Range(0f,1f)] public float integrity01 = 0.20f;

    void OnEnable()
    {
        if (spiritManager != null) spiritManager.OnOwnedChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (spiritManager != null) spiritManager.OnOwnedChanged -= Refresh;
    }

    public void Refresh()
    {
        // Left: live Spirits, placeholders for others
        int spirits = (spiritManager != null && spiritManager.OwnedSpiritIds != null)
                      ? spiritManager.OwnedSpiritIds.Count
                      : 0;
        if (valueSpirits)    valueSpirits.text    = spirits.ToString();
        if (valuePassedOver) valuePassedOver.text = passedOver.ToString();
        if (valueGraves)     valueGraves.text     = graves.ToString();

        // Right: placeholder meters
        if (serenityFill) serenityFill.fillAmount = Mathf.Clamp01(serenity01);

        if (appetiteFill) appetiteFill.fillAmount = Mathf.Clamp01(appetite01);

        if (integrityFill) integrityFill.fillAmount = Mathf.Clamp01(integrity01);
    }
}