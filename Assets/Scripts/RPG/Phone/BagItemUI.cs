using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button useButton;

    private ItemData itemData;
    private Action<string> requestUse;

    private void Awake()
    {
        if (useButton != null)
            useButton.onClick.AddListener(OnUseClicked);
    }

    public void Setup(
        ItemData data,
        int quantity,
        Action<string> onRequestUse)
    {
        if (data == null ||
            nameText == null ||
            descriptionText == null ||
            quantityText == null ||
            useButton == null)
        {
            Debug.LogError("背包卡片：数据或 UI 引用未配置。", this);
            return;
        }

        itemData = data;
        requestUse = onRequestUse;

        nameText.text = data.itemName;
        quantityText.text = $"×{quantity}";

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.gameObject.SetActive(data.icon != null);
        }

        RefreshAction();
    }

    public void RefreshAction()
    {
        if (itemData == null)
            return;

        InventoryManager inventory = InventoryManager.Instance;

        bool isEquipment =
            itemData.itemType == ItemType.Weapon ||
            itemData.itemType == ItemType.Armor;

        bool supported =
            itemData.itemType == ItemType.Consumable ||
            isEquipment;

        descriptionText.text = itemData.description;

        if (inventory != null)
        {
            // 装备显示未装备数量，消耗品显示剩余数量
            int count = isEquipment
                ? inventory.GetAvailableCount(itemData.itemId)
                : inventory.GetCount(itemData.itemId);

            quantityText.text = $"×{count}";
        }

        useButton.gameObject.SetActive(supported);

        useButton.interactable =
            supported &&
            inventory != null &&
            inventory.CanUseItemOnAnyMember(itemData.itemId);
    }

    private void OnUseClicked()
    {
        if (itemData != null && useButton.interactable)
            requestUse?.Invoke(itemData.itemId);
    }

    private void OnDestroy()
    {
        if (useButton != null)
            useButton.onClick.RemoveListener(OnUseClicked);
    }
}