using System;
using System.Collections.Generic;
using UnityEngine;

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    [Header("主角：引用已有 CharacterData")]
    [SerializeField] private CharacterData mainCharacter;

    [Header("新游戏时额外加入的队员，主角会自动加入")]
    [SerializeField]
    private CharacterData[] startingMembers =
        new CharacterData[0];

    [Header("团队初始资金")]
    [Min(0)]
    [SerializeField] private int startingMoney;

    private Dictionary<string, CharacterData> definitions =
        new Dictionary<string, CharacterData>();

    private readonly Dictionary<string, PartyMemberState> states =
        new Dictionary<string, PartyMemberState>();

    private readonly List<string> memberIds = new List<string>();

    private bool initialized;
    private int money;

    public IReadOnlyList<string> MemberIds => memberIds;
    public int Money => money;

    public string MainCharacterId =>
        mainCharacter != null ? mainCharacter.characterID : "";

    public event Action OnPartyChanged;
    public event Action OnStateChanged;

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
        EnsureInitialized();
    }

    public bool EnsureInitialized()
    {
        return initialized || ResetParty();
    }

    public bool IsMainCharacter(string characterId)
    {
        return !string.IsNullOrWhiteSpace(characterId) &&
               characterId == MainCharacterId;
    }

    public bool ResetParty()
    {
        CharacterStateManager characterManager =
            CharacterStateManager.Instance;

        if (mainCharacter == null ||
            string.IsNullOrWhiteSpace(mainCharacter.characterID) ||
            characterManager == null ||
            characterManager.allCharacterDatas == null)
        {
            Debug.LogError(
                "队伍：请配置主角，并检查 CharacterStateManager 的角色资料。",
                this);
            return false;
        }

        var registered = new Dictionary<string, CharacterData>();

        foreach (CharacterData data in characterManager.allCharacterDatas)
        {
            if (data == null ||
                string.IsNullOrWhiteSpace(data.characterID))
            {
                continue;
            }

            if (registered.ContainsKey(data.characterID))
            {
                Debug.LogWarning(
                    $"队伍：角色 ID 重复：{data.characterID}",
                    this);
                continue;
            }

            registered.Add(data.characterID, data);
        }

        if (!registered.TryGetValue(MainCharacterId, out CharacterData hero) ||
            hero != mainCharacter)
        {
            Debug.LogError(
                "队伍：主角必须引用 CharacterStateManager 中的同一份角色资产。",
                this);
            return false;
        }

        definitions = registered;
        states.Clear();
        memberIds.Clear();

        money = Mathf.Max(0, startingMoney);

        JoinInternal(MainCharacterId);

        if (startingMembers != null)
        {
            foreach (CharacterData data in startingMembers)
            {
                if (data == null)
                    continue;

                if (!definitions.TryGetValue(
                        data.characterID, out CharacterData registeredData) ||
                    registeredData != data)
                {
                    Debug.LogWarning(
                        $"队伍：初始队员 {data.name} 没有注册为同一份角色资产。",
                        this);
                    continue;
                }

                JoinInternal(data.characterID);
            }
        }

        initialized = true;

        OnPartyChanged?.Invoke();
        OnStateChanged?.Invoke();

        return true;
    }

    public CharacterData GetCharacterData(string characterId)
    {
        if (!EnsureInitialized() ||
            string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        definitions.TryGetValue(characterId, out CharacterData data);
        return data;
    }

    public PartyMemberState GetMemberState(string characterId)
    {
        if (!EnsureInitialized() ||
            string.IsNullOrWhiteSpace(characterId) ||
            !memberIds.Contains(characterId))
        {
            return null;
        }

        states.TryGetValue(characterId, out PartyMemberState state);
        return state;
    }

    public bool JoinMember(string characterId)
    {
        if (!EnsureInitialized() || !JoinInternal(characterId))
            return false;

        OnPartyChanged?.Invoke();
        return true;
    }

    private bool JoinInternal(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId) ||
            memberIds.Contains(characterId) ||
            !definitions.TryGetValue(characterId, out CharacterData data))
        {
            return false;
        }

        // 离队后重新加入，保留本局已经产生的属性变化。
        if (!states.ContainsKey(characterId))
            states.Add(characterId, new PartyMemberState(data));

        memberIds.Add(characterId);
        return true;
    }

    public bool LeaveMember(string characterId)
    {
        if (!EnsureInitialized() ||
            IsMainCharacter(characterId) ||
            !memberIds.Remove(characterId))
        {
            return false;
        }

        if (states.TryGetValue(characterId, out PartyMemberState state))
        {
            state.WeaponData = null;
            state.ArmorData = null;
        }

        OnPartyChanged?.Invoke();
        OnStateChanged?.Invoke();

        return true;
    }

    public bool TakeDamage(string characterId, int amount)
    {
        PartyMemberState state = GetMemberState(characterId);

        if (state == null || amount <= 0 || state.CurrentHP <= 0)
            return false;

        state.CurrentHP = Mathf.Max(0, state.CurrentHP - amount);

        OnStateChanged?.Invoke();
        return true;
    }

    public bool RestoreHP(string characterId, int amount)
    {
        PartyMemberState state = GetMemberState(characterId);

        if (state == null || amount <= 0 ||
            state.CurrentHP >= state.MaxHP)
        {
            return false;
        }

        state.CurrentHP += Mathf.Min(
            amount, state.MaxHP - state.CurrentHP);

        OnStateChanged?.Invoke();
        return true;
    }

    public bool TrySpendSP(string characterId, int amount)
    {
        PartyMemberState state = GetMemberState(characterId);

        if (state == null || amount < 0 || amount > state.CurrentSP)
            return false;

        if (amount == 0)
            return true;

        state.CurrentSP -= amount;

        OnStateChanged?.Invoke();
        return true;
    }

    public bool RestoreSP(string characterId, int amount)
    {
        PartyMemberState state = GetMemberState(characterId);

        if (state == null || amount <= 0 ||
            state.CurrentSP >= state.MaxSP)
        {
            return false;
        }

        state.CurrentSP += Mathf.Min(
            amount, state.MaxSP - state.CurrentSP);

        OnStateChanged?.Invoke();
        return true;
    }

    public bool AddMoney(int amount)
    {
        if (!EnsureInitialized() ||
            amount <= 0 ||
            amount > int.MaxValue - money)
        {
            return false;
        }

        money += amount;

        OnStateChanged?.Invoke();
        return true;
    }

    public bool TrySpendMoney(int amount)
    {
        if (!EnsureInitialized() || amount < 0 || amount > money)
            return false;

        if (amount == 0)
            return true;

        money -= amount;

        OnStateChanged?.Invoke();
        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetEquippedCount(string itemId)
    {
        if (!initialized || string.IsNullOrWhiteSpace(itemId))
            return 0;

        int count = 0;

        foreach (string characterId in memberIds)
        {
            if (!states.TryGetValue(characterId, out PartyMemberState state))
                continue;

            if (state.WeaponItemId == itemId)
                count++;

            if (state.ArmorItemId == itemId)
                count++;
        }

        return count;
    }

    public bool IsEquippedByMember(string characterId, string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        PartyMemberState state = GetMemberState(characterId);

        return state != null &&
               (state.WeaponItemId == itemId ||
                state.ArmorItemId == itemId);
    }

    public bool CanEquipItem(string characterId, string itemId)
    {
        PartyMemberState state = GetMemberState(characterId);
        InventoryManager inventory = InventoryManager.Instance;

        if (state == null || inventory == null)
            return false;

        ItemData data = inventory.GetItemData(itemId);

        if (data == null ||
            (data.itemType != ItemType.Weapon &&
             data.itemType != ItemType.Armor))
        {
            return false;
        }

        if (IsEquippedByMember(characterId, itemId))
            return false;

        return inventory.GetAvailableCount(itemId) > 0;
    }

    public bool TryEquipItem(string characterId, string itemId)
    {
        if (!CanEquipItem(characterId, itemId))
            return false;

        PartyMemberState state = GetMemberState(characterId);
        ItemData data = InventoryManager.Instance.GetItemData(itemId);

        if (data.itemType == ItemType.Weapon)
            state.WeaponData = data;
        else
            state.ArmorData = data;

        OnStateChanged?.Invoke();
        return true;
    }

    public bool TryUnequipItem(string characterId, ItemType slotType)
    {
        PartyMemberState state = GetMemberState(characterId);

        if (state == null)
            return false;

        if (slotType == ItemType.Weapon)
        {
            if (state.WeaponData == null)
                return false;

            state.WeaponData = null;
        }
        else if (slotType == ItemType.Armor)
        {
            if (state.ArmorData == null)
                return false;

            state.ArmorData = null;
        }
        else
        {
            return false;
        }

        OnStateChanged?.Invoke();
        return true;
    }

    public void UnequipAll()
    {
        if (!initialized)
            return;

        bool changed = false;

        foreach (PartyMemberState state in states.Values)
        {
            if (state.WeaponData == null && state.ArmorData == null)
                continue;

            state.WeaponData = null;
            state.ArmorData = null;
            changed = true;
        }

        if (changed)
            OnStateChanged?.Invoke();
    }

    public void GetEquipmentPreview(
        string characterId,
        ItemData item,
        ItemActionType action,
        out int attack,
        out int defense)
    {
        PartyMemberState state = GetMemberState(characterId);

        attack = state != null ? state.Attack : 0;
        defense = state != null ? state.Defense : 0;

        if (state == null || item == null)
            return;

        ItemData weapon = state.WeaponData;
        ItemData armor = state.ArmorData;

        if (item.itemType == ItemType.Weapon)
        {
            weapon = action == ItemActionType.Unequip ? null : item;
        }
        else if (item.itemType == ItemType.Armor)
        {
            armor = action == ItemActionType.Unequip ? null : item;
        }
        else
        {
            return;
        }

        attack = state.GetAttackWith(weapon, armor);
        defense = state.GetDefenseWith(weapon, armor);
    }
}