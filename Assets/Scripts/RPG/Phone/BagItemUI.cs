using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagItemUI : MonoBehaviour
{
    [Header("物品显示")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image iconImage;

    [Header("操作")]
    [SerializeField] private Button useButton;

    private ItemData itemData;

    private void Awake()
    {
        if (useButton != null)
            useButton.onClick.AddListener(OnUseClicked);
    }

    public void Setup(ItemData data, int quantity)
    {
        if (data == null ||
            nameText == null ||
            descriptionText == null ||
            quantityText == null ||
            useButton == null)
        {
            Debug.LogError("背包物品卡片：数据或 UI 引用未配置。", this);
            return;
        }

        itemData = data;

        nameText.text = data.itemName;
        descriptionText.text = data.description;
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
        if (itemData == null || useButton == null)
            return;

        bool isConsumable = itemData.itemType == ItemType.Consumable;

        useButton.gameObject.SetActive(isConsumable);

        if (!isConsumable)
            return;

        InventoryManager inventory = InventoryManager.Instance;

        useButton.interactable =
            inventory != null && inventory.CanUseItem(itemData.itemId);
    }

    private void OnUseClicked()
    {
        if (itemData == null)
            return;

        InventoryManager inventory = InventoryManager.Instance;

        if (inventory != null)
            inventory.TryUseItem(itemData.itemId);
    }

    private void OnDestroy()
    {
        if (useButton != null)
            useButton.onClick.RemoveListener(OnUseClicked);
    }
}