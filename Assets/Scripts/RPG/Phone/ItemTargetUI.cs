using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTargetUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text spText;
    [SerializeField] private Button selectButton;

    public string CharacterId { get; private set; }

    private Action<string> onSelected;
    private bool ready;

    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(SelectTarget);
    }

    public void Initialize(
        CharacterData data,
        PartyMemberState state,
        bool canUse,
        Action<string> callback)
    {
        ready =
            data != null &&
            state != null &&
            nameText != null &&
            avatarImage != null &&
            hpText != null &&
            spText != null &&
            selectButton != null;

        if (!ready)
        {
            Debug.LogError("目标条目：数据或 UI 引用未配置。", this);
            return;
        }

        CharacterId = data.characterID;
        onSelected = callback;

        RefreshDisplay(data, state, canUse);
    }

    public void RefreshDisplay(
        CharacterData data,
        PartyMemberState state,
        bool canUse)
    {
        if (!ready)
            return;

        if (data == null || state == null)
        {
            selectButton.interactable = false;
            return;
        }

        nameText.text = CharacterDisplayUtility.GetName(data);

        avatarImage.sprite = data.contactAvatar;
        avatarImage.preserveAspect = true;
        avatarImage.enabled = data.contactAvatar != null;

        hpText.text = $"HP：{state.CurrentHP} / {state.MaxHP}";
        spText.text = $"SP：{state.CurrentSP} / {state.MaxSP}";

        selectButton.interactable = canUse;
    }

    private void SelectTarget()
    {
        if (ready && selectButton.interactable)
            onSelected?.Invoke(CharacterId);
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(SelectTarget);
    }
}