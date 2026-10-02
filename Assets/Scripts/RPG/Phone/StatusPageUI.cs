using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StatusPageUI : MonoBehaviour
{
    [Header("角色列表")]
    [SerializeField] private Transform content;
    [SerializeField] private CharacterStatusCardUI cardPrefab;

    [Header("团队资金")]
    [SerializeField] private TMP_Text moneyText;

    private PartyManager party;
    private bool hasStarted;

    private readonly List<CharacterStatusCardUI> cards =
        new List<CharacterStatusCardUI>();

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
        if (party != null)
        {
            party.OnPartyChanged -= RebuildCards;
            party.OnStateChanged -= RefreshState;
        }

        party = null;
    }

    private void Connect()
    {
        if (party != null)
            return;

        if (content == null || cardPrefab == null || moneyText == null)
        {
            Debug.LogError("队伍状态页：列表或资金引用未配置。", this);
            return;
        }

        PartyManager manager = PartyManager.Instance;

        if (manager == null || !manager.EnsureInitialized())
        {
            Debug.LogError(
                "队伍状态页：队伍未初始化，请从 MenuScene 开始游戏。",
                this);
            return;
        }

        party = manager;

        party.OnPartyChanged += RebuildCards;
        party.OnStateChanged += RefreshState;

        RebuildCards();
    }

    private void RebuildCards()
    {
        foreach (CharacterStatusCardUI card in cards)
        {
            if (card == null)
                continue;

            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }

        cards.Clear();

        foreach (string characterId in party.MemberIds)
        {
            CharacterStatusCardUI card =
                Instantiate(cardPrefab, content, false);

            card.gameObject.SetActive(true);
            card.Initialize(party, characterId);

            cards.Add(card);
        }

        RefreshState();
    }

    private void RefreshState()
    {
        if (party == null)
            return;

        foreach (CharacterStatusCardUI card in cards)
        {
            if (card != null)
                card.RefreshView();
        }

        moneyText.text = $"资金：{party.Money}";
    }
}