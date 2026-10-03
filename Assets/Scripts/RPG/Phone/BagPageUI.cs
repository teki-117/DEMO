using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BagPageUI : MonoBehaviour
{
    [Header("列表")]
    [SerializeField] private Transform content;
    [SerializeField] private BagItemUI itemPrefab;
    [SerializeField] private TMP_Text emptyText;

    [Header("选择使用目标")]
    [SerializeField] private ItemTargetPanelUI targetPanel;

    private InventoryManager inventory;
    private PartyManager party;
    private bool hasStarted;

    private readonly List<BagItemUI> itemViews =
        new List<BagItemUI>();

    private void Start()
    {
        hasStarted = true;
        Connect();
    }

    private void OnEnable()
    {
        if (hasStarted)
            Connect();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;

        if (party != null)
        {
            party.OnStateChanged -= RefreshItemActions;
            party.OnPartyChanged -= RefreshItemActions;
        }

        if (targetPanel != null)
            targetPanel.Close();

        inventory = null;
        party = null;
    }

    private void Connect()
    {
        if (inventory != null)
            return;

        if (content == null ||
            itemPrefab == null ||
            emptyText == null ||
            targetPanel == null)
        {
            Debug.LogError("背包页：列表或目标窗口引用未配置。", this);
            return;
        }

        InventoryManager bag = InventoryManager.Instance;
        PartyManager team = PartyManager.Instance;

        if (bag == null || team == null || !team.EnsureInitialized())
        {
            Debug.LogError("背包页：请从 MenuScene 开始游戏。", this);
            return;
        }

        inventory = bag;
        party = team;

        inventory.OnInventoryChanged += Refresh;
        party.OnStateChanged += RefreshItemActions;
        party.OnPartyChanged += RefreshItemActions;

        Refresh();
    }

    private void Refresh()
    {
        if (inventory == null)
            return;

        foreach (BagItemUI view in itemViews)
        {
            if (view == null)
                continue;

            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }

        itemViews.Clear();

        foreach (ItemData data in inventory.AllItems)
        {
            int quantity = inventory.GetCount(data.itemId);

            if (quantity <= 0)
                continue;

            BagItemUI view = Instantiate(itemPrefab, content, false);

            view.gameObject.SetActive(true);
            view.Setup(data, quantity, OpenTargetPanel);

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

    private void OpenTargetPanel(string itemId)
    {
        if (targetPanel != null)
            targetPanel.Open(itemId);
    }
}