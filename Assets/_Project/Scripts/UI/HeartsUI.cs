using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HeartsUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth playerHealth;
    public Image heartTemplate;   // disabled prefab
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("Layout")]
    [Min(1)] public int heartSize = 48;
    public int spacing = 8;

    readonly List<Image> hearts = new List<Image>();

    void OnEnable()
    {
        if (playerHealth)
            playerHealth.onHealthChanged.AddListener(OnHealthChanged);
        Rebuild();
    }

    void OnDisable()
    {
        if (playerHealth)
            playerHealth.onHealthChanged.RemoveListener(OnHealthChanged);
    }

    void OnHealthChanged(int current, int max) => Rebuild();

    void Rebuild()
    {
        if (!heartTemplate || !playerHealth) return;

        // clear existing
        foreach (var img in hearts)
            if (img) Destroy(img.gameObject);
        hearts.Clear();

        int max = Mathf.Max(1, playerHealth.Max);
        int cur = Mathf.Clamp(playerHealth.Current, 0, max);

        int pulsingIndex = (cur > 0) ? Mathf.Min(cur, max) - 1 : -1;

        for (int i = 0; i < max; i++)
        {
            var go = Instantiate(heartTemplate.gameObject, heartTemplate.transform.parent);
            go.name = $"Heart_{i}";
            go.SetActive(true);

            var img = go.GetComponent<Image>();
            hearts.Add(img);

            // choose sprite
            img.sprite = (i < cur) ? fullHeartSprite : emptyHeartSprite;
            img.color = Color.white; // keep alpha 1 so sprites decide look

            // optional size
            var rt = img.rectTransform;
            if (heartSize > 0)
                rt.sizeDelta = new Vector2(heartSize, heartSize);

            var pulse = go.GetComponent<HeartPulse>();
            if (!pulse)
                pulse = go.AddComponent<HeartPulse>();
            pulse.SetPulsing(i == pulsingIndex);
        }

        var hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg) hlg.spacing = spacing;
    }
}
