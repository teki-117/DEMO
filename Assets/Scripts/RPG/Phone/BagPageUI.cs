using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BagPageUI : MonoBehaviour
{
    [Header("列表")]
    [SerializeField] private Transform content;
    [SerializeField] private BagItemUI itemPrefab;
    [SerializeField] private TMP_Text emptyText;

    private InventoryManager inventory;
    private PlayerStateManager player;

    private readonly List<BagItemUI> itemViews =
        new List<BagItemUI>();

    private void OnEnable()
    {
        if (content == null || itemPrefab == null || emptyText == null)
        {
            Debug.LogError("背包页：UI 引用未配置。", this);
            return;
        }

        inventory = InventoryManager.Instance;
        player = PlayerStateManager.Instance;

        if (inventory == null || player == null)
        {
            Debug.LogError(
                "背包页：缺少管理器，请从 MenuScene 开始游戏。",
                this);
            return;
        }

        inventory.OnInventoryChanged += Refresh;
        player.OnPlayerStateChanged += RefreshItemActions;

        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;

        if (player != null)
            player.OnPlayerStateChanged -= RefreshItemActions;

        inventory = null;
        player = null;
    }

    private void Refresh()
    {
        if (inventory == null)
            return;

        ClearItems();

        foreach (ItemData data in inventory.AllItems)
        {
            int quantity = inventory.GetCount(data.itemId);

            if (quantity <= 0)
                continue;

            BagItemUI view = Instantiate(itemPrefab, content, false);

            view.gameObject.SetActive(true);
            view.Setup(data, quantity);

            itemViews.Add(view);
        }

        emptyText.gameObject.SetActive(itemViews.Count == 0);
    }

    private void RefreshItemActions()
    {
        foreach (BagItemUI view in itemViews)
        {
            if (view != null)
                view.RefreshAction();
        }
    }

    private void ClearItems()
    {
        foreach (BagItemUI view in itemViews)
        {
            if (view == null)
                continue;

            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }

        itemViews.Clear();
    }
}