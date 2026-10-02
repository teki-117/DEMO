using UnityEngine;

public enum ItemType
{
    Consumable,   // 消耗品
    Weapon,       // 武器
    Armor,        // 防具
    KeyItem       // 关键物品
}

[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("物品信息")]
    public string itemId;
    public string itemName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;
    public ItemType itemType;

    [Header("消耗品效果")]
    [Min(0)] public int restoreHP;
    [Min(0)] public int restoreSP;

    [Header("装备加成")]
    [Min(0)] public int attackBonus;
    [Min(0)] public int defenseBonus;
}