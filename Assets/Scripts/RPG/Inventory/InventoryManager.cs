using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("本 Demo 使用的物品")]
    [SerializeField] private ItemData[] allItemDatas = new ItemData[0];

    [Header("新游戏初始物品")]
    [SerializeField] private ItemStack[] startingItems = new ItemStack[0];

    [Header("本局背包数据")]
    [SerializeField] private InventoryState state;

    private readonly Dictionary<string, ItemData> definitions =
        new Dictionary<string, ItemData>();

    private readonly List<ItemData> registeredItems =
        new List<ItemData>();

    public IReadOnlyList<ItemData> AllItems => registeredItems;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResetInventory();
    }

    public void ResetInventory()
    {
        if (PartyManager.Instance != null)
            PartyManager.Instance.UnequipAll();

        definitions.Clear();
        registeredItems.Clear();
        state = new InventoryState();

        if (allItemDatas != null)
        {
            foreach (var data in allItemDatas)
            {
                if (data == null ||
                    string.IsNullOrWhiteSpace(data.itemId))
                {
                    Debug.LogWarning("背包：跳过空物品或缺少 ID 的物品。", this);
                    continue;
                }

                if (definitions.ContainsKey(data.itemId))
                {
                    Debug.LogWarning(
                        $"背包：物品 ID 重复，跳过 {data.itemId}。",
                        this);
                    continue;
                }

                if (data.restoreHP < 0 || data.restoreSP < 0 ||
                    data.attackBonus < 0 || data.defenseBonus < 0)
                {
                    Debug.LogWarning(
                        $"背包：物品 {data.itemId} 的效果配置无效。",
                        this);
                    continue;
                }

                definitions.Add(data.itemId, data);
                registeredItems.Add(data);
            }
        }

        if (startingItems != null)
        {
            foreach (var item in startingItems)
            {
                if (item == null) continue;

                if (!AddItemInternal(item.itemId, item.quantity))
                {
                    Debug.LogWarning(
                        $"背包：初始物品配置无效，ID={item.itemId}，" +
                        $"数量={item.quantity}。",
                        this);
                }
            }
        }

        OnInventoryChanged?.Invoke();
    }

    public ItemData GetItemData(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        return definitions.TryGetValue(itemId, out var data)
            ? data
            : null;
    }

    public int GetCount(string itemId)
    {
        var stack = FindStack(itemId);
        return stack != null ? stack.quantity : 0;
    }

    public bool AddItem(string itemId, int amount)
    {
        if (!AddItemInternal(itemId, amount))
            return false;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryRemoveItem(string itemId, int amount)
    {
        if (!RemoveItemInternal(itemId, amount))
            return false;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetAvailableCount(string itemId)
    {
        int equippedCount = PartyManager.Instance != null
            ? PartyManager.Instance.GetEquippedCount(itemId)
            : 0;

        return Mathf.Max(0, GetCount(itemId) - equippedCount);
    }

    public bool TryGetAction(
        string itemId,
        string characterId,
        out ItemActionType action)
    {
        action = ItemActionType.UseConsumable;

        ItemData data = GetItemData(itemId);
        PartyManager party = PartyManager.Instance;

        if (data == null || party == null || GetCount(itemId) < 1)
            return false;

        PartyMemberState target = party.GetMemberState(characterId);

        if (target == null)
            return false;

        if (data.itemType == ItemType.Consumable)
        {
            bool canRestoreHP =
                data.restoreHP > 0 && target.CurrentHP < target.MaxHP;

            bool canRestoreSP =
                data.restoreSP > 0 && target.CurrentSP < target.MaxSP;

            return canRestoreHP || canRestoreSP;
        }

        if (data.itemType != ItemType.Weapon &&
            data.itemType != ItemType.Armor)
        {
            return false;
        }

        if (party.IsEquippedByMember(characterId, itemId))
        {
            action = ItemActionType.Unequip;
            return true;
        }

        action = ItemActionType.Equip;
        return party.CanEquipItem(characterId, itemId);
    }

    public bool CanUseItem(string itemId)
    {
        PartyManager party = PartyManager.Instance;

        return party != null &&
               CanUseItem(itemId, party.MainCharacterId);
    }

    public bool CanUseItem(string itemId, string characterId)
    {
        return TryGetAction(itemId, characterId, out _);
    }

    public bool CanUseItemOnAnyMember(string itemId)
    {
        PartyManager party = PartyManager.Instance;

        if (party == null || !party.EnsureInitialized())
            return false;

        foreach (string characterId in party.MemberIds)
        {
            if (CanUseItem(itemId, characterId))
                return true;
        }

        return false;
    }

    // 保留旧调试脚本使用的入口。
    public bool TryUseItem(string itemId)
    {
        PartyManager party = PartyManager.Instance;

        return party != null &&
               TryUseItem(itemId, party.MainCharacterId);
    }

    public bool TryUseItem(string itemId, string characterId)
    {
        if (!TryGetAction(itemId, characterId, out ItemActionType action))
            return false;

        return TryPerformAction(itemId, characterId, action);
    }

    // 确认时要求实际操作仍与用户选择的操作一致。
    public bool TryPerformAction(
        string itemId,
        string characterId,
        ItemActionType expectedAction)
    {
        if (!TryGetAction(
                itemId, characterId, out ItemActionType currentAction) ||
            currentAction != expectedAction)
        {
            return false;
        }

        ItemData data = GetItemData(itemId);
        PartyManager party = PartyManager.Instance;

        if (currentAction == ItemActionType.Equip)
            return party.TryEquipItem(characterId, itemId);

        if (currentAction == ItemActionType.Unequip)
            return party.TryUnequipItem(characterId, data.itemType);

        if (!RemoveItemInternal(itemId, 1))
            return false;

        party.RestoreHP(characterId, data.restoreHP);
        party.RestoreSP(characterId, data.restoreSP);

        OnInventoryChanged?.Invoke();
        return true;
    }

    private bool AddItemInternal(string itemId, int amount)
    {
        if (amount <= 0 || GetItemData(itemId) == null)
            return false;

        var stack = FindStack(itemId);
        int currentCount = stack != null ? stack.quantity : 0;

        if (amount > int.MaxValue - currentCount)
            return false;

        if (stack == null)
        {
            state.items.Add(new ItemStack
            {
                itemId = itemId,
                quantity = amount
            });
        }
        else
        {
            stack.quantity += amount;
        }

        return true;
    }

    private bool RemoveItemInternal(string itemId, int amount)
    {
        if (amount <= 0)
            return false;

        ItemStack stack = FindStack(itemId);

        if (stack == null ||
            stack.quantity < amount ||
            GetAvailableCount(itemId) < amount)
        {
            return false;
        }

        stack.quantity -= amount;

        if (stack.quantity == 0)
            state.items.Remove(stack);

        return true;
    }

    private ItemStack FindStack(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        return state.items.Find(
            item => item != null && item.itemId == itemId);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}