using UnityEngine;

public class PartyMemberState
{
    public string CharacterId { get; private set; }

    public int Level { get; internal set; }
    public int EXP { get; internal set; }

    public int CurrentHP { get; internal set; }
    public int MaxHP { get; internal set; }

    public int CurrentSP { get; internal set; }
    public int MaxSP { get; internal set; }

    public int Attack { get; internal set; }
    public int Defense { get; internal set; }

    public PartyMemberState(CharacterData data)
    {
        CharacterId = data.characterID;

        Level = Mathf.Max(1, data.startingLevel);
        EXP = 0;

        MaxHP = Mathf.Max(1, data.startingMaxHP);
        CurrentHP = MaxHP;

        MaxSP = Mathf.Max(0, data.startingMaxSP);
        CurrentSP = MaxSP;

        Attack = Mathf.Max(0, data.startingAttack);
        Defense = Mathf.Max(0, data.startingDefense);
    }
}