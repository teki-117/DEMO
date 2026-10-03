using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTargetPanelUI : MonoBehaviour
{
    [Header("目标窗口")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Button cancelButton;

    [Header("队员列表")]
    [SerializeField] private Transform content;
    [SerializeField] private ItemTargetUI targetPrefab;

    [Header("确认窗口")]
    [SerializeField] private GameObject confirmationRoot;
    [SerializeField] private TMP_Text confirmationText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button backButton;

    private InventoryManager inventory;
    private PartyManager party;
    private string itemId;

    private string selectedCharacterId;
    private ItemActionType selectedAction;
    private bool isExecuting;

    private readonly List<ItemTargetUI> targets =
        new List<ItemTargetUI>();

    private void Awake()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSelection);

        if (backButton != null)
            backButton.onClick.AddListener(HideConfirmation);
    }

    public void Open(string selectedItemId)
    {
        Close();

        if (titleText == null ||
            hintText == null ||
            cancelButton == null ||
            content == null ||
            targetPrefab == null ||
            confirmationRoot == null ||
            confirmationText == null ||
            confirmButton == null ||
            backButton == null)
        {
            Debug.LogError("物品操作窗口：UI 引用未配置。", this);
            return;
        }

        InventoryManager bag = InventoryManager.Instance;
        PartyManager team = PartyManager.Instance;

        if (bag == null || team == null || !team.EnsureInitialized())
            return;

        ItemData data = bag.GetItemData(selectedItemId);

        if (data == null ||
            data.itemType == ItemType.KeyItem ||
            bag.GetCount(selectedItemId) < 1)
        {
            return;
        }

        inventory = bag;
        party = team;
        itemId = selectedItemId;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        inventory.OnInventoryChanged += RefreshTargets;
        party.OnStateChanged += RefreshTargets;
        party.OnPartyChanged += RebuildTargets;

        RebuildTargets();
    }

    public void Close()
    {
        ClearContext();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ClearContext();
    }

    private void ClearContext()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshTargets;

        if (party != null)
        {
            party.OnStateChanged -= RefreshTargets;
            party.OnPartyChanged -= RebuildTargets;
        }

        HideConfirmation();

        inventory = null;
        party = null;
        itemId = null;
        isExecuting = false;
    }

    private void RebuildTargets()
    {
        if (isExecuting || inventory == null || party == null)
            return;

        HideConfirmation();

        foreach (ItemTargetUI target in targets)
        {
            if (target == null)
                continue;

            target.gameObject.SetActive(false);
            Destroy(target.gameObject);
        }

        targets.Clear();

        foreach (string characterId in party.MemberIds)
        {
            CharacterData data = party.GetCharacterData(characterId);
            PartyMemberState state = party.GetMemberState(characterId);

            if (data == null || state == null)
                continue;

            ItemTargetUI target =
                Instantiate(targetPrefab, content, false);

            target.gameObject.SetActive(true);

            target.Initialize(
                data,
                state,
                inventory.CanUseItem(itemId, characterId),
                OnTargetSelected);

            targets.Add(target);
        }

        RefreshTargets();
    }

    private void RefreshTargets()
    {
        if (isExecuting || inventory == null || party == null)
            return;

        ItemData data = inventory.GetItemData(itemId);
        int quantity = inventory.GetCount(itemId);

        if (data == null || quantity < 1)
        {
            Close();
            return;
        }

        titleText.text = $"{data.itemName} ×{quantity}";

        bool anyAvailable = false;

        foreach (ItemTargetUI target in targets)
        {
            if (target == null)
                continue;

            string characterId = target.CharacterId;
            bool canUse = inventory.CanUseItem(itemId, characterId);

            anyAvailable |= canUse;

            target.RefreshDisplay(
                party.GetCharacterData(characterId),
                party.GetMemberState(characterId),
                canUse);
        }

        if (data.itemType == ItemType.Consumable)
        {
            hintText.text = anyAvailable
                ? "选择队员，再确认使用。"
                : "当前队员都不需要这个物品。";
        }
        else
        {
            hintText.text =
                $"空闲：{inventory.GetAvailableCount(itemId)} 件。" +
                "选择持有者可卸下，其他队员需有空闲装备。";
        }

        RefreshConfirmation();
    }

    private void OnTargetSelected(string characterId)
    {
        if (inventory == null ||
            !inventory.TryGetAction(
                itemId, characterId, out ItemActionType action))
        {
            RefreshTargets();
            return;
        }

        selectedCharacterId = characterId;
        selectedAction = action;

        confirmationRoot.SetActive(true);
        confirmationRoot.transform.SetAsLastSibling();

        RefreshConfirmation();
    }

    private void RefreshConfirmation()
    {
        if (inventory == null ||
            party == null ||
            string.IsNullOrEmpty(selectedCharacterId))
        {
            return;
        }

        bool valid = inventory.TryGetAction(
            itemId,
            selectedCharacterId,
            out ItemActionType currentAction);

        valid = valid && currentAction == selectedAction;

        confirmButton.interactable = valid;

        confirmationText.text = valid
            ? BuildConfirmationText()
            : "目标或物品状态已变化。\n请返回重新选择。";
    }

    private string BuildConfirmationText()
    {
        ItemData data = inventory.GetItemData(itemId);

        CharacterData character =
            party.GetCharacterData(selectedCharacterId);

        PartyMemberState state =
            party.GetMemberState(selectedCharacterId);

        string name = CharacterDisplayUtility.GetName(character);

        if (selectedAction == ItemActionType.UseConsumable)
        {
            int afterHP = (int)Math.Min(
                state.MaxHP, (long)state.CurrentHP + data.restoreHP);

            int afterSP = (int)Math.Min(
                state.MaxSP, (long)state.CurrentSP + data.restoreSP);

            return
                $"对{name}使用{data.itemName}？\n\n" +
                $"HP：{state.CurrentHP} → {afterHP}\n" +
                $"SP：{state.CurrentSP} → {afterSP}\n\n" +
                "消耗数量：1";
        }

        party.GetEquipmentPreview(
            selectedCharacterId,
            data,
            selectedAction,
            out int afterAttack,
            out int afterDefense);

        string operation = selectedAction == ItemActionType.Equip
            ? "装备"
            : "卸下";

        return
            $"为{name}{operation}{data.itemName}？\n\n" +
            $"攻击：{state.Attack} → {afterAttack}\n" +
            $"防御：{state.Defense} → {afterDefense}";
    }

    private void ConfirmSelection()
    {
        if (isExecuting ||
            inventory == null ||
            string.IsNullOrEmpty(selectedCharacterId))
        {
            return;
        }

        InventoryManager bag = inventory;
        string selectedItemId = itemId;
        string characterId = selectedCharacterId;
        ItemActionType action = selectedAction;

        bool success;

        isExecuting = true;

        try
        {
            success = bag.TryPerformAction(
                selectedItemId, characterId, action);
        }
        finally
        {
            isExecuting = false;
        }

        if (success)
        {
            Close();
            return;
        }

        RefreshTargets();
    }

    private void HideConfirmation()
    {
        selectedCharacterId = null;

        if (confirmationRoot != null)
            confirmationRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Close);

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmSelection);

        if (backButton != null)
            backButton.onClick.RemoveListener(HideConfirmation);
    }
}