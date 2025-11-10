using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CultNotificationsController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ToastManager toastManager;

    [Header("UI")]
    [SerializeField] private Transform listRoot;     // Section_Notifications/Scroll_Notifications/Viewport/ListRoot
    [SerializeField] private GameObject rowPrefab;   // NotificationRow prefab
    [SerializeField, Min(1)] private int maxRows = 30;

    private readonly List<GameObject> rows = new();

    void OnEnable()
    {
        RebuildFromHistory();
        if (toastManager != null) toastManager.OnToastAdded += HandleToastAdded;
    }

    void OnDisable()
    {
        if (toastManager != null) toastManager.OnToastAdded -= HandleToastAdded;
    }

    void RebuildFromHistory()
    {
        ClearAll();
        if (toastManager == null || toastManager.History == null) return;

        // Build from newest to oldest (end of list is newest based on our push)
        var hist = toastManager.History;
        for (int i = hist.Count - 1; i >= 0; i--)
            AddRow(hist[i]);
    }

    void HandleToastAdded(ToastManager.ToastHistoryEntry e)
    {
        // Add newest on top
        AddRow(e, insertAtTop: true);
        TrimIfNeeded();
    }

    void AddRow(ToastManager.ToastHistoryEntry e, bool insertAtTop = false)
    {
        if (!rowPrefab || !listRoot) return;

        var go = Instantiate(rowPrefab);
        if (insertAtTop && rows.Count > 0)
            go.transform.SetSiblingIndex(0);
        go.transform.SetParent(listRoot, false);
        rows.Insert(insertAtTop ? 0 : rows.Count, go);

        // Bind
        var icon = go.transform.Find("Icon")?.GetComponent<Image>();
        var amount = go.transform.Find("Amount")?.GetComponent<TMP_Text>();
        var msg = go.transform.Find("Message")?.GetComponent<TMP_Text>();

        if (icon) icon.sprite = e.icon;
        if (amount)
        {
            amount.text = $"+{e.delta}";
            amount.color = e.color;
        }
        if (msg) msg.text = e.message;
    }

    void TrimIfNeeded()
    {
        while (rows.Count > Mathf.Max(1, maxRows))
        {
            var last = rows[rows.Count - 1];
            rows.RemoveAt(rows.Count - 1);
            if (last) Destroy(last);
        }
    }

    void ClearAll()
    {
        for (int i = 0; i < rows.Count; i++) if (rows[i]) Destroy(rows[i]);
        rows.Clear();
    }
}