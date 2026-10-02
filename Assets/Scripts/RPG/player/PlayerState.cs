using System;

// 保存本局玩家数据，不负责执行游戏逻辑
[Serializable]
public class PlayerState
{
    public int level = 1;
    public int exp;

    public int currentHP;
    public int maxHP;

    public int currentSP;
    public int maxSP;

    public int baseAttack;
    public int baseDefense;

    public int money;
}