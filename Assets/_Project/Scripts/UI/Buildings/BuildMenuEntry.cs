using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenuEntry : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private Button button;

    [Header("Cost Row")]
    [SerializeField] private CostEntryUI[] costEntries = Array.Empty<CostEntryUI>();
    [SerializeField] private CostEntryUI freeEntry;
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.red;

    [Serializable]
    struct CostEntryUI
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI countLabel;

        public void Show(Sprite sprite, string text, Color textColor, bool showIcon = true)
        {
            if (root) root.SetActive(true);
            if (icon)
            {
                if (showIcon && sprite != null)
                {
                    icon.enabled = true;
                    icon.sprite = sprite;
                }
                else
                {
                    icon.enabled = false;
                    icon.sprite = null;
                }
            }
            if (countLabel)
            {
                countLabel.text = text ?? string.Empty;
                countLabel.color = textColor;
            }
        }

        public void Hide()
        {
            if (root) root.SetActive(false);
        }
    }

    BuildingSO _building;
    BuildMenu _owner;
    Inventory _inventory;

    public void Bind(BuildingSO building, Inventory inventory, BuildMenu owner)
    {
        _building = building;
        _owner = owner;
        _inventory = inventory;

        if (iconImage)
        {
            iconImage.sprite = building?.Icon;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (nameLabel)
            nameLabel.text = building?.DisplayName ?? "Unnamed";

        RefreshCostRow(building);

        if (button)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }
    }

    void RefreshCostRow(BuildingSO building)
    {
        bool hasAnyCosts = false;
        int slotIndex = 0;

        var costs = building?.Costs;
        if (costs != null)
        {
            for (int i = 0; i < costs.Count && slotIndex < costEntries.Length; i++)
            {
                var cost = costs[i];
                if (cost == null || cost.item == null || cost.amount <= 0)
                    continue;

                hasAnyCosts = true;
                var sprite = cost.item.Icon;
                bool hasEnough = _inventory && cost.item && _inventory.CountOf(cost.item) >= cost.amount;
                var color = hasEnough ? affordableColor : unaffordableColor;
                costEntries[slotIndex].Show(sprite, cost.amount.ToString(), color, showIcon: true);
                slotIndex++;
            }
        }

        for (int i = slotIndex; i < costEntries.Length; i++)
            costEntries[i].Hide();

        if (!hasAnyCosts)
            freeEntry.Show(null, "-", affordableColor, showIcon: false);
        else
            freeEntry.Hide();
    }

    void OnClicked()
    {
        if (_building == null || _owner == null)
            return;

        if (!HasResources())
            return;

        _owner.HandleEntryClicked(_building);
    }

    bool HasResources()
    {
        var costs = _building?.Costs;
        if (costs == null || costs.Count == 0)
            return true;

        if (!_inventory)
            return false;

        for (int i = 0; i < costs.Count; i++)
        {
            var cost = costs[i];
            if (cost == null || cost.item == null) continue;
            if (_inventory.CountOf(cost.item) < cost.amount)
                return false;
        }
        return true;
    }
}
