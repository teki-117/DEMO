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

    public bool CanUseItem(string itemId)
    {
        ItemData data = GetItemData(itemId);

        if (data == null ||
            data.itemType != ItemType.Consumable ||
            GetCount(itemId) < 1)
        {
            return false;
        }

        PlayerStateManager player = PlayerStateManager.Instance;

        if (player == null)
            return false;

        bool canRestoreHP =
            data.restoreHP > 0 && player.CurrentHP < player.MaxHP;

        bool canRestoreSP =
            data.restoreSP > 0 && player.CurrentSP < player.MaxSP;

        return canRestoreHP || canRestoreSP;
    }

    public bool TryUseItem(string itemId)
    {
        PlayerStateManager player = PlayerStateManager.Instance;

        if (player == null)
        {
            Debug.LogWarning("背包：缺少 PlayerStateManager。", this);
            return false;
        }

        if (!CanUseItem(itemId))
            return false;

        ItemData data = GetItemData(itemId);

        if (!RemoveItemInternal(itemId, 1))
            return false;

        player.RestoreHP(data.restoreHP);
        player.RestoreSP(data.restoreSP);

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

        var stack = FindStack(itemId);

        if (stack == null || stack.quantity < amount)
            return false;

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