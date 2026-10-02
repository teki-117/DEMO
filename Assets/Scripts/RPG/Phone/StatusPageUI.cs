using TMPro;
using UnityEngine;

public class StatusPageUI : MonoBehaviour
{
    [Header("玩家信息")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;

    [Header("属性")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text spText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text moneyText;

    private PlayerStateManager player;

    private void OnEnable()
    {
        if (playerNameText == null ||
            levelText == null ||
            expText == null ||
            hpText == null ||
            spText == null ||
            attackText == null ||
            defenseText == null ||
            moneyText == null)
        {
            Debug.LogError("状态页：文字引用未配置。", this);
            return;
        }

        player = PlayerStateManager.Instance;

        if (player == null)
        {
            Debug.LogError(
                "状态页：缺少 PlayerStateManager，请从 MenuScene 开始游戏。",
                this);
            return;
        }

        player.OnPlayerStateChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (player != null)
            player.OnPlayerStateChanged -= Refresh;

        player = null;
    }

    private void Refresh()
    {
        if (player == null)
            return;

        string playerName = GameManager.Instance != null
            ? GameManager.Instance.playerName
            : null;

        playerNameText.text = string.IsNullOrWhiteSpace(playerName)
            ? "主角"
            : playerName;

        levelText.text = $"等级：{player.Level}";
        expText.text = $"经验：{player.EXP}";

        hpText.text = $"HP：{player.CurrentHP} / {player.MaxHP}";
        spText.text = $"SP：{player.CurrentSP} / {player.MaxSP}";

        attackText.text = $"攻击：{player.Attack}";
        defenseText.text = $"防御：{player.Defense}";
        moneyText.text = $"金钱：{player.Money}";
    }
}