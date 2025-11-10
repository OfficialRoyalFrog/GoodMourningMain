using UnityEngine;

public enum ItemCategory
{
    Currency,
    Food,
    Item
}

[CreateAssetMenu(menuName = "Game/Item", fileName = "ItemSO_")]
public class ItemSO : ScriptableObject
{
    [SerializeField] private string id;          // unique, stable (e.g., "wood", "stone")
    [SerializeField] private string displayName; // UI name
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int maxStack = 99;

    [Header("Classification")]
    [SerializeField] private ItemCategory category = ItemCategory.Item;

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public int MaxStack => maxStack;
    public ItemCategory Category => category;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = name.Replace("ItemSO_", string.Empty).Trim().ToLowerInvariant();
        if (maxStack < 1) maxStack = 1;
    }
#endif
}