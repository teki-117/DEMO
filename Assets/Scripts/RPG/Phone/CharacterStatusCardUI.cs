using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStatusCardUI : MonoBehaviour
{
    [Header("角色显示")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image portraitImage;

    [Header("角色属性")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text spText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;

    private PartyManager party;
    private string characterId;
    private bool ready;

    public void Initialize(PartyManager manager, string id)
    {
        party = manager;
        characterId = id;

        ready =
            party != null &&
            playerNameText != null &&
            portraitImage != null &&
            levelText != null &&
            expText != null &&
            hpText != null &&
            spText != null &&
            attackText != null &&
            defenseText != null;

        if (!ready)
        {
            Debug.LogError("角色状态卡片：UI 引用未配置。", this);
            return;
        }

        RefreshView();
    }

    public void RefreshView()
    {
        if (!ready || party == null)
            return;

        CharacterData data = party.GetCharacterData(characterId);
        PartyMemberState state = party.GetMemberState(characterId);

        if (data == null || state == null)
            return;

        playerNameText.text = CharacterDisplayUtility.GetName(data);

        portraitImage.sprite = data.contactAvatar;
        portraitImage.preserveAspect = true;
        portraitImage.enabled = data.contactAvatar != null;

        levelText.text = $"等级：{state.Level}";
        expText.text = $"经验：{state.EXP}";

        hpText.text = $"HP：{state.CurrentHP} / {state.MaxHP}";
        spText.text = $"SP：{state.CurrentSP} / {state.MaxSP}";

        attackText.text = $"攻击：{state.Attack}";
        defenseText.text = $"防御：{state.Defense}";
    }
}