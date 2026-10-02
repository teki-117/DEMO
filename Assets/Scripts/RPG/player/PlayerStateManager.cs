using System;
using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    public static PlayerStateManager Instance { get; private set; }

    public event Action OnPlayerStateChanged;

    private PartyManager subscribedParty;

    private PartyManager Party => PartyManager.Instance;

    private PartyMemberState State =>
        Party != null
            ? Party.GetMemberState(Party.MainCharacterId)
            : null;

    public int Level => State != null ? State.Level : 1;
    public int EXP => State != null ? State.EXP : 0;

    public int CurrentHP => State != null ? State.CurrentHP : 0;
    public int MaxHP => State != null ? State.MaxHP : 0;

    public int CurrentSP => State != null ? State.CurrentSP : 0;
    public int MaxSP => State != null ? State.MaxSP : 0;

    public int Attack => State != null ? State.Attack : 0;
    public int Defense => State != null ? State.Defense : 0;

    public int Money => Party != null ? Party.Money : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (BindParty())
            subscribedParty.EnsureInitialized();
    }

    private bool BindParty()
    {
        PartyManager manager = PartyManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "Íæ¼Ò£ºÈ±ÉÙ PartyManager£¬ÇëÔÚ MenuScene ÅäÖÃ¡£",
                this);
            return false;
        }

        if (subscribedParty == manager)
            return true;

        if (subscribedParty != null)
            subscribedParty.OnStateChanged -= NotifyChanged;

        subscribedParty = manager;
        subscribedParty.OnStateChanged += NotifyChanged;

        return true;
    }

    public void ResetPlayer()
    {
        if (BindParty())
            subscribedParty.ResetParty();
    }

    public bool TakeDamage(int amount) =>
        Party != null &&
        Party.TakeDamage(Party.MainCharacterId, amount);

    public bool RestoreHP(int amount) =>
        Party != null &&
        Party.RestoreHP(Party.MainCharacterId, amount);

    public bool TrySpendSP(int amount) =>
        Party != null &&
        Party.TrySpendSP(Party.MainCharacterId, amount);

    public bool RestoreSP(int amount) =>
        Party != null &&
        Party.RestoreSP(Party.MainCharacterId, amount);

    public bool AddMoney(int amount) =>
        Party != null && Party.AddMoney(amount);

    public bool TrySpendMoney(int amount) =>
        Party != null && Party.TrySpendMoney(amount);

    private void NotifyChanged()
    {
        OnPlayerStateChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (subscribedParty != null)
            subscribedParty.OnStateChanged -= NotifyChanged;

        if (Instance == this)
            Instance = null;
    }
}