using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SpiritRadialActionSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject actionButtonPrefab;  // prefab for each wedge button
    [SerializeField] private Transform actionsRoot;          // where to spawn under (RingRoot/Actions)
    [SerializeField] private SpiritManager spiritManager;    // assign Systems/SpiritManager
    [SerializeField] private SpiritRadialController controller;
    [SerializeField] private SpiritActionSetSO actionSet;  // assign ActionSet_Default here if SpiritManager doesn't expose it

    [Header("Layout")]
    [SerializeField, Range(80f, 220f)] private float radius = 140f;
    [SerializeField] private float startAngle = 90f; // top
    [SerializeField] private float spacingDeg = 360f; // full circle distribution

    public void Populate(string categoryId, string spiritId)
    {
        if (!actionsRoot) return;
        Clear();

        // Prefer explicit assignment of the action set; fall back to SpiritManager when available
        var set = actionSet;
        if (set == null && spiritManager != null)
        {
            // If your SpiritManager exposes a getter later, you can replace this with that call.
            // For now, we only rely on the serialized field.
        }
        if (set == null || set.Actions == null)
        {
            Debug.LogWarning("[Radial] No ActionSet assigned on SpiritRadialActionSpawner.");
            return;
        }

        var filtered = new List<SpiritActionDefSO>();
        var actions = set.Actions;
        for (int i = 0; i < actions.Count; i++)
        {
            var a = actions[i];
            if (a == null) continue;
            if (a.Category.ToString().Equals(categoryId, System.StringComparison.OrdinalIgnoreCase))
                filtered.Add(a);
        }

        if (filtered.Count == 0)
        {
            Debug.Log("[Radial] No actions for " + categoryId);
            return;
        }

        float step = spacingDeg / filtered.Count;
        for (int i = 0; i < filtered.Count; i++)
        {
            var def = filtered[i];
            float angle = startAngle - step * i;
            Vector2 pos = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);

            var btnGO = Instantiate(actionButtonPrefab, actionsRoot);
            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;

            var label = btnGO.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = def.DisplayName;

            var button = btnGO.GetComponent<Button>();
            if (button)
            {
                button.onClick.AddListener(() =>
                {
                    spiritManager.TryExecuteAction(spiritId, def, out var reason);
                    controller.Hide();
                });
            }
        }
    }

    public void Clear()
    {
        if (!actionsRoot) return;
        for (int i = actionsRoot.childCount - 1; i >= 0; i--)
            Destroy(actionsRoot.GetChild(i).gameObject);
    }
}