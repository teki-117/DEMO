using System.Collections.Generic;

public class CharacterSaveData
{
    public string characterID;
    public float positionX;
    public float scale;
    public string expressionName;
    public string motionKey;
}

public class SaveData
{
    public string savedStoryFileName;
    public int savedLine;
    public byte[] savedScreenshotData;
    public LinkedList<ExcelData> savedHistoryRecords;
    public string savedBackgroundImg;
    public string savedBackgroundMusic;
    public List<CharacterSaveData> savedCharacters;
    public string savedPlayerName;
    public Dictionary<string, int> savedAffection;

    // ---------- V2 新增；旧存档里没有这些字段，读出来是 null ----------

    // 存档时所在的场景：GameScene（剧情）或 MapScene（地图）。null 视为剧情。
    public string savedScene;

    // 剧情结束后回到哪个场景（从地图进学校时是 MapScene）。
    public string savedStoryReturnScene;

    // 世界、时间、任务、背包、队伍。null 时按新游戏初始值处理。
    public RpgSaveData rpg;
}

public class RpgSaveData
{
    // 时间
    public int day = 1;
    public TimePeriod period = TimePeriod.Morning;

    // 世界
    public string locationId = Constants.LOCATION_MAP;
    public bool hasPendingAction;
    public List<string> unlockedLocations = new List<string>();
    public bool afterBattleStoryPlayed;
    public int nextSchoolChapter;   // 学校下一章在列表里的位置

    // 任务
    public List<QuestState> quests = new List<QuestState>();

    // 背包：物品 ID → 数量（包含正在装备的）
    public Dictionary<string, int> items = new Dictionary<string, int>();

    // 队伍
    public int money;
    public List<PartyMemberSaveData> members = new List<PartyMemberSaveData>();
}

public class PartyMemberSaveData
{
    public string characterId;
    public int level = 1;
    public int exp;
    public int currentHP;
    public int maxHP;
    public int currentSP;
    public int maxSP;
    public int baseAttack;
    public int baseDefense;
    public string weaponItemId;
    public string armorItemId;
}