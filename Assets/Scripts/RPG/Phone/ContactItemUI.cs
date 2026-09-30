using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 只负责一条联系人的显示，不修改角色状态。
public class ContactItemUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI affectionText;

    public CharacterData Data { get; private set; }

    public void Initialize(CharacterData data)
    {
        Data = data;

        if (data == null) return;

        if (nameText != null)
        {
            nameText.text = string.IsNullOrWhiteSpace(data.displayName)
                ? data.characterID
                : data.displayName;
        }

        if (avatarImage != null)
        {
            avatarImage.sprite = data.contactAvatar;
            avatarImage.preserveAspect = true;
            avatarImage.enabled = data.contactAvatar != null;
        }
    }

    public void SetAffection(int affection)
    {
        if (affectionText != null)
        {
            affectionText.text = $"好感度：{affection}";
        }
    }
}
