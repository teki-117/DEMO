using System;
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

    public int BaseAttack { get; internal set; }
    public int BaseDefense { get; internal set; }

    internal ItemData WeaponData { get; set; }
    internal ItemData ArmorData { get; set; }

    public string WeaponItemId =>
        WeaponData != null ? WeaponData.itemId : "";

    public string ArmorItemId =>
        ArmorData != null ? ArmorData.itemId : "";

    public string WeaponName =>
        WeaponData != null ? WeaponData.itemName : "нч";

    public string ArmorName =>
        ArmorData != null ? ArmorData.itemName : "нч";

    public int Attack => GetAttackWith(WeaponData, ArmorData);
    public int Defense => GetDefenseWith(WeaponData, ArmorData);

    public PartyMemberState(CharacterData data)
    {
        CharacterId = data.characterID;

        Level = Mathf.Max(1, data.startingLevel);
        EXP = 0;

        MaxHP = Mathf.Max(1, data.startingMaxHP);
        CurrentHP = MaxHP;

        MaxSP = Mathf.Max(0, data.startingMaxSP);
        CurrentSP = MaxSP;

        BaseAttack = Mathf.Max(0, data.startingAttack);
        BaseDefense = Mathf.Max(0, data.startingDefense);
    }

    public int GetAttackWith(ItemData weapon, ItemData armor)
    {
        return Calculate(BaseAttack, weapon, armor, true);
    }

    public int GetDefenseWith(ItemData weapon, ItemData armor)
    {
        return Calculate(BaseDefense, weapon, armor, false);
    }

    private static int Calculate(
        int baseValue,
        ItemData weapon,
        ItemData armor,
        bool calculateAttack)
    {
        long result = Mathf.Max(0, baseValue);

        if (weapon != null)
        {
            result += Mathf.Max(
                0,
                calculateAttack
                    ? weapon.attackBonus
                    : weapon.defenseBonus);
        }

        if (armor != null)
        {
            result += Mathf.Max(
                0,
                calculateAttack
                    ? armor.attackBonus
                    : armor.defenseBonus);
        }

        return (int)Math.Min(int.MaxValue, result);
    }
}