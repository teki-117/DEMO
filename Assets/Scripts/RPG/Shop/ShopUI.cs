using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 地图里的商店面板：商品配置、购买和显示都在这里。
// 金钱只走 PartyManager，物品只走 InventoryManager，商店自己不保存状态。
public class ShopUI : MonoBehaviour
{
    [Serializable]
    public class ShopGoods
    {
        [Tooltip("ItemData 里的 itemId。")]
        public string itemId;

        [Min(0)]
        public int price;
    }

    [Header("商品（itemId 和价格）")]
    [SerializeField] private ShopGoods[] goods =
    {
        new ShopGoods { itemId = "hp_potion", price = 30 },
        new ShopGoods { itemId = "training_armor", price = 80 },
        new ShopGoods { itemId = "iron_sword", price = 150 }
    };

    [Header("界面")]
    [Tooltip("整个商店面板，平时隐藏。")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Transform content;
    [SerializeField] private ShopItemUI itemPrefab;
    [SerializeField] private Button closeButton;

    [Header("提示文字（可选）")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 2f;

    [Header("音效（可选）")]
    [SerializeField] private AudioClip buySfx;
    [SerializeField] private AudioClip failSfx;

    // 商店打开时，手机不响应 Tab。
    public static bool IsOpen { get; private set; }

    private readonly List<ShopItemUI> rows = new List<ShopItemUI>();
    private float messageHideTime;
    private bool subscribed;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        IsOpen = false;
    }

    private void OnDestroy()
    {
        Unsubscribe();
        IsOpen = false;
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (messageText != null &&
            messageText.gameObject.activeSelf &&
            Time.unscaledTime >= messageHideTime)
        {
            messageText.gameObject.SetActive(false);
        }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
#endif
    }

    public void Open()
    {
        if (IsOpen)
            return;

        if (panelRoot == null || content == null || itemPrefab == null)
        {
            Debug.LogError("商店：panelRoot、content、itemPrefab 没有配置。", this);
            return;
        }

        if (PartyManager.Instance == null || InventoryManager.Instance == null)
        {
            Debug.LogError("商店需要 PartyManager 和 InventoryManager，请从 MenuScene 启动。", this);
            return;
        }

        PartyManager.Instance.EnsureInitialized();

        IsOpen = true;
        panelRoot.SetActive(true);

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        BuildRows();
        Subscribe();
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        Unsubscribe();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (WorldManager.Instance != null)
            WorldManager.Instance.LeaveLocation();
    }

    private void Subscribe()
    {
        if (subscribed)
            return;

        PartyManager.Instance.OnStateChanged += Refresh;
        InventoryManager.Instance.OnInventoryChanged += Refresh;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (PartyManager.Instance != null)
            PartyManager.Instance.OnStateChanged -= Refresh;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;

        subscribed = false;
    }

    private void BuildRows()
    {
        foreach (ShopItemUI row in rows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        rows.Clear();

        if (goods == null)
            return;

        InventoryManager inventory = InventoryManager.Instance;

        foreach (ShopGoods g in goods)
        {
            ItemData data = g != null ? inventory.GetItemData(g.itemId) : null;

            if (data == null)
            {
                Debug.LogWarning($"商店：找不到物品 {g?.itemId}，检查 InventoryManager 的物品列表。", this);
                continue;
            }

            ShopItemUI row = Instantiate(itemPrefab, content);
            ShopGoods captured = g;
            row.Setup(data, g.price, () => TryBuy(captured));
            rows.Add(row);
        }
    }

    private void Refresh()
    {
        if (!IsOpen)
            return;

        PartyManager party = PartyManager.Instance;
        InventoryManager inventory = InventoryManager.Instance;

        if (party == null || inventory == null)
            return;

        if (moneyText != null)
            moneyText.text = $"金钱：{party.Money}";

        foreach (ShopItemUI row in rows)
        {
            if (row == null || row.Data == null)
                continue;

            int owned = inventory.GetCount(row.Data.itemId);
            row.Refresh(owned, IsEquipment(row.Data) && owned > 0, party.Money >= row.Price);
        }
    }

    private void TryBuy(ShopGoods g)
    {
        PartyManager party = PartyManager.Instance;
        InventoryManager inventory = InventoryManager.Instance;

        if (party == null || inventory == null)
            return;

        ItemData data = inventory.GetItemData(g.itemId);

        if (data == null)
            return;

        // 装备限购一件。
        if (IsEquipment(data) && inventory.GetCount(data.itemId) > 0)
        {
            Fail($"已经有{data.itemName}了。");
            return;
        }

        if (!party.TrySpendMoney(g.price))
        {
            Fail("金钱不足。");
            return;
        }

        if (!inventory.AddItem(data.itemId, 1))
        {
            // 放不进背包就把钱退回去。
            party.AddMoney(g.price);
            Fail("买不了这个物品。");
            return;
        }

        ShowMessage($"买下了{data.itemName}。");
        PlaySfx(buySfx);
        Refresh();
    }

    private static bool IsEquipment(ItemData data)
    {
        return data.itemType == ItemType.Weapon || data.itemType == ItemType.Armor;
    }

    private void Fail(string message)
    {
        ShowMessage(message);
        PlaySfx(failSfx);
    }

    private void ShowMessage(string message)
    {
        if (messageText == null)
        {
            Debug.Log($"商店：{message}");
            return;
        }

        messageText.text = message;
        messageText.gameObject.SetActive(true);
        messageHideTime = Time.unscaledTime + messageDuration;
    }

    private static void PlaySfx(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clip, 1f);
    }
}
