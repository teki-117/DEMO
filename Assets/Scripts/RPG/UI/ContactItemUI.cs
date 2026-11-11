using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContactItemUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI affectionText;

    public CharacterData Data { get; private set; }

    public void Initialize(CharacterData data)
    {
        Data = data;
        RefreshProfile();
    }

    private void RefreshProfile()
    {
        if (Data == null)
            return;

        if (nameText != null)
            nameText.text = CharacterDisplayUtility.GetName(Data);

        if (avatarImage != null)
        {
            avatarImage.sprite = Data.contactAvatar;
            avatarImage.preserveAspect = true;
            avatarImage.enabled = Data.contactAvatar != null;
        }
    }

    public void SetAffection(int affection)
    {
        RefreshProfile();

        if (affectionText != null)
            affectionText.text = $"好感度：{affection}";
    }
}