using System;
using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    public static PlayerStateManager Instance { get; private set; }

    [Header("新游戏初始配置")]
    [SerializeField, Min(1)] private int startingMaxHP = 100;
    [SerializeField, Min(0)] private int startingMaxSP = 30;
    [SerializeField, Min(0)] private int startingAttack = 10;
    [SerializeField, Min(0)] private int startingDefense = 5;
    [SerializeField, Min(0)] private int startingMoney = 0;

    [Header("本局运行数据")]
    [SerializeField] private PlayerState state;

    // 后续手机状态页监听这个事件。
    public event Action OnPlayerStateChanged;

    public int Level => state.level;
    public int EXP => state.exp;

    public int CurrentHP => state.currentHP;
    public int MaxHP => state.maxHP;

    public int CurrentSP => state.currentSP;
    public int MaxSP => state.maxSP;

    public int Attack => state.baseAttack;
    public int Defense => state.baseDefense;
    public int Money => state.money;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResetPlayer();
    }

    // 开始新游戏时恢复初始状态。
    public void ResetPlayer()
    {
        int hp = Mathf.Max(1, startingMaxHP);
        int sp = Mathf.Max(0, startingMaxSP);

        state = new PlayerState
        {
            currentHP = hp,
            maxHP = hp,

            currentSP = sp,
            maxSP = sp,

            baseAttack = Mathf.Max(0, startingAttack),
            baseDefense = Mathf.Max(0, startingDefense),
            money = Mathf.Max(0, startingMoney)
        };

        NotifyChanged();
    }

    // amount 是已经计算好的伤害量。
    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || state.currentHP == 0)
            return false;

        state.currentHP -= Mathf.Min(amount, state.currentHP);
        NotifyChanged();
        return true;
    }

    public bool RestoreHP(int amount)
    {
        if (amount <= 0 || state.currentHP >= state.maxHP)
            return false;

        int restored = Mathf.Min(
            amount,
            state.maxHP - state.currentHP);

        state.currentHP += restored;
        NotifyChanged();
        return true;
    }

    public bool TrySpendSP(int amount)
    {
        if (amount < 0 || state.currentSP < amount)
            return false;

        if (amount == 0)
            return true;

        state.currentSP -= amount;
        NotifyChanged();
        return true;
    }

    public bool RestoreSP(int amount)
    {
        if (amount <= 0 || state.currentSP >= state.maxSP)
            return false;

        int restored = Mathf.Min(
            amount,
            state.maxSP - state.currentSP);

        state.currentSP += restored;
        NotifyChanged();
        return true;
    }

    public bool AddMoney(int amount)
    {
        if (amount <= 0 || amount > int.MaxValue - state.money)
            return false;

        state.money += amount;
        NotifyChanged();
        return true;
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || state.money < amount)
            return false;

        if (amount == 0)
            return true;

        state.money -= amount;
        NotifyChanged();
        return true;
    }

    private void NotifyChanged()
    {
        OnPlayerStateChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}