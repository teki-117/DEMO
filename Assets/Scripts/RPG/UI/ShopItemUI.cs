using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 商店里的一行商品。只负责显示，点击购买交给 ShopUI。
public class ShopItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [Tooltip("显示已持有数量（可选）。")]
    [SerializeField] private TMP_Text ownedText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button buyButton;
    [Tooltip("购买按钮上的文字（可选），已拥有时显示“已拥有”。")]
    [SerializeField] private TMP_Text buyButtonText;

    [Header("价格颜色")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color tooExpensiveColor = new Color(1f, 0.4f, 0.4f);

    private Action onBuy;

    public ItemData Data { get; private set; }
    public int Price { get; private set; }

    private void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(() => onBuy?.Invoke());
    }

    public void Setup(ItemData data, int price, Action buyCallback)
    {
        Data = data;
        Price = price;
        onBuy = buyCallback;

        if (nameText != null)
            nameText.text = data.itemName;

        if (descriptionText != null)
            descriptionText.text = data.description;

        if (priceText != null)
            priceText.text = $"{price} 元";

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.gameObject.SetActive(data.icon != null);
        }

        if (nameText == null || priceText == null || buyButton == null)
            Debug.LogError("商店商品行：nameText、priceText、buyButton 没有配置。", this);
    }

    // soldOut：装备已拥有，不能再买。affordable：钱够不够，只影响价格颜色。
    public void Refresh(int owned, bool soldOut, bool affordable)
    {
        if (ownedText != null)
            ownedText.text = $"持有 {owned}";

        if (priceText != null)
            priceText.color = affordable ? affordableColor : tooExpensiveColor;

        if (buyButton != null)
            buyButton.interactable = !soldOut;

        if (buyButtonText != null)
            buyButtonText.text = soldOut ? "已拥有" : "购买";
    }
}
