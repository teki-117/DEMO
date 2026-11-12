using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public enum BattleResult
{
    Completed,
    Retreated,
    Defeated
}

public class WorldManager : MonoBehaviour
{
    private bool hasPendingAction;
    public static WorldManager Instance { get; private set; }

    [SerializeField] private string currentLocationId = Constants.LOCATION_MAP;

    [Header("地点解锁")]
    [Tooltip("新游戏时锁住的地点，要由剧情里的 location_unlock 解锁。")]
    [SerializeField] private string[] lockedAtStart = { Constants.LOCATION_RPG, Constants.LOCATION_SCHOOL };

    [Header("学校章节")]
    [Tooltip("每次点学校按顺序播放下一章（剧情文件名，不含 .xlsx）。全部播完后学校没有新剧情。")]
    [SerializeField] private string[] schoolChapters = { "3", "4" };

    [Header("家")]
    [Tooltip("点地图上的家进入的场景。出来时推进一个时段。")]
    [SerializeField] private string homeScene = Constants.LIVE2D_GAME_SCENE;

    [Header("通关后剧情")]
    [Tooltip("第一次完成神社关卡后播放的剧情文件名（不含 .xlsx）。留空则直接回地图。")]
    [SerializeField] private string afterBattleStoryFile = "2-1";

    private readonly HashSet<string> unlockedLocations = new HashSet<string>();
    private bool afterBattleStoryPlayed;
    private int nextSchoolChapter;

    public string CurrentLocationId => currentLocationId;

    // 学校还有没播过的章节。
    public bool HasSchoolChapter =>
        schoolChapters != null && nextSchoolChapter < schoolChapters.Length;

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

    // 开始新游戏时重置世界状态。
    public void ResetWorld()
    {
        currentLocationId = Constants.LOCATION_MAP;
        hasPendingAction = false;
        unlockedLocations.Clear();
        afterBattleStoryPlayed = false;
        nextSchoolChapter = 0;
    }

    // ---------- 存档：把各 Manager 的数据收齐 / 分发回去 ----------

    public RpgSaveData CaptureRpgSave()
    {
        var data = new RpgSaveData
        {
            locationId = currentLocationId,
            hasPendingAction = hasPendingAction,
            unlockedLocations = new List<string>(unlockedLocations),
            afterBattleStoryPlayed = afterBattleStoryPlayed,
            nextSchoolChapter = nextSchoolChapter
        };

        if (GameTimeManager.Instance != null)
        {
            data.day = GameTimeManager.Instance.CurrentDay;
            data.period = GameTimeManager.Instance.CurrentPeriod;
        }

        if (QuestManager.Instance != null)
            data.quests = QuestManager.Instance.ExportStates();

        if (InventoryManager.Instance != null)
            data.items = InventoryManager.Instance.ExportItems();

        if (PartyManager.Instance != null)
        {
            data.money = PartyManager.Instance.Money;
            data.members = PartyManager.Instance.ExportMembers();
        }

        return data;
    }

    // data 为 null（旧存档）时，全部回到新游戏的初始状态。
    public void RestoreRpgSave(RpgSaveData data)
    {
        if (data == null)
        {
            Debug.Log("存档里没有 RPG 数据（旧存档），世界、时间、任务、背包、队伍按新游戏初始值处理。");

            ResetWorld();

            if (GameTimeManager.Instance != null)
                GameTimeManager.Instance.ResetTime();

            if (QuestManager.Instance != null)
                QuestManager.Instance.ResetQuests();

            if (PartyManager.Instance != null)
                PartyManager.Instance.ResetParty();

            if (InventoryManager.Instance != null)
                InventoryManager.Instance.ResetInventory();

            return;
        }

        currentLocationId = string.IsNullOrEmpty(data.locationId) ? Constants.LOCATION_MAP : data.locationId;
        hasPendingAction = data.hasPendingAction;
        afterBattleStoryPlayed = data.afterBattleStoryPlayed;
        nextSchoolChapter = Mathf.Max(0, data.nextSchoolChapter);

        unlockedLocations.Clear();
        if (data.unlockedLocations != null)
            unlockedLocations.UnionWith(data.unlockedLocations);

        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.SetTime(data.day, data.period);

        if (QuestManager.Instance != null)
            QuestManager.Instance.ImportStates(data.quests);

        // 先背包后队伍：队伍读档时要按物品 ID 找回装备。
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ImportItems(data.items);

        if (PartyManager.Instance != null)
            PartyManager.Instance.ImportState(data.money, data.members);
    }

    public bool IsLocationLocked(string locationId)
    {
        if (lockedAtStart == null || unlockedLocations.Contains(locationId))
            return false;

        return System.Array.IndexOf(lockedAtStart, locationId) >= 0;
    }

    // 剧情的 location_unlock 指令调用。重复解锁没有影响。
    public void UnlockLocation(string rawLocationId)
    {
        string locationId = rawLocationId?.Trim();

        if (string.IsNullOrEmpty(locationId))
        {
            Debug.LogError("剧情地点解锁指令缺少地点 ID。");
            return;
        }

        if (unlockedLocations.Add(locationId))
            Debug.Log($"剧情解锁地点：{locationId}");
    }

    // 地图界面统一通过这个入口选择地点。地点还没解锁时返回 false。
    public bool EnterLocation(string locationId)
    {
        if (IsLocationLocked(locationId))
        {
            Debug.Log($"地点 {locationId} 还没有解锁。");
            return false;
        }

        switch (locationId)
        {
            case Constants.LOCATION_SCHOOL:
                return EnterSchool();

            case Constants.LOCATION_RPG:
                EnterBattle();
                break;

            case Constants.LOCATION_SHOP:
                // 商店面板由 MapController 打开；购物不推进时间。
                currentLocationId = locationId;
                break;

            case Constants.LOCATION_HOME:
                EnterHome();
                break;
        }

        return true;
    }

    // 关闭商店等地图内的界面后回到地图，不推进时间。
    public void LeaveLocation()
    {
        currentLocationId = Constants.LOCATION_MAP;
    }

    // 学校：播放下一章。章节播完（剧情结束回地图）时才算这一章看过。
    // 没有新章节时返回 false，地图会提示。
    private bool EnterSchool()
    {
        if (hasPendingAction)
            return true;

        if (!HasSchoolChapter)
            return false;

        var gm = GameManager.Instance;

        if (gm == null)
        {
            Debug.LogError("找不到 GameManager，请从原来的游戏入口启动。");
            return true;
        }

        currentLocationId = Constants.LOCATION_SCHOOL;

        gm.PrepareStory(schoolChapters[nextSchoolChapter].Trim());
        gm.storyReturnScene = Constants.MAP_SCENE;
        hasPendingAction = true;

        SceneManager.LoadScene(Constants.GAME_SCENE);
        return true;
    }

    // 家：进入小场景，出门时由 MuSceneController 调 LeaveHome 结算时间。
    private void EnterHome()
    {
        if (hasPendingAction)
            return;

        if (!Application.CanStreamedLevelBeLoaded(homeScene))
        {
            Debug.LogError($"找不到场景 {homeScene}，请检查构建场景列表。");
            return;
        }

        currentLocationId = Constants.LOCATION_HOME;
        hasPendingAction = true;

        SceneManager.LoadScene(homeScene);
    }

    // 从家出门：推进一个时段，回地图。
    public bool LeaveHome()
    {
        if (currentLocationId != Constants.LOCATION_HOME)
            return false;

        CompleteCurrentAction();
        currentLocationId = Constants.LOCATION_MAP;

        if (GameManager.Instance != null)
            GameManager.Instance.currentScene = Constants.MAP_SCENE;

        SceneManager.LoadScene(Constants.MAP_SCENE);
        return true;
    }

    public void CompleteCurrentAction()
    {
        // 没有待结算行动时，直接返回。
        if (!hasPendingAction)
        {
            return;
        }

        if (GameTimeManager.Instance == null)
        {
            Debug.LogError("缺少 GameTimeManager，无法结算行动时间。");
            return;
        }

        // 先标记为已结算，避免同一次行动重复结算。
        hasPendingAction = false;

        // 学校的一章完整播完，下次点学校进入下一章。
        if (currentLocationId == Constants.LOCATION_SCHOOL)
            nextSchoolChapter++;

        // 向任务系统报告一次完整的地点行动。
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ReportLocationActionCompleted(
                currentLocationId);
        }

        // 一次行动推进一个时间段。
        GameTimeManager.Instance.AdvanceTime();
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 从地图神社进入战斗场景。
    private void EnterBattle()
    {
        // 已有行动进行中时，防止重复进入。
        if (hasPendingAction)
            return;

        if (GameManager.Instance == null ||
            GameTimeManager.Instance == null ||
            PartyManager.Instance == null)
        {
            Debug.LogError("战斗入口缺少全局管理器，请从正式启动流程进入。");
            return;
        }

        PartyManager party = PartyManager.Instance;

        if (!party.EnsureInitialized())
            return;

        PartyMemberState hero =
            party.GetMemberState(party.MainCharacterId);

        if (hero == null)
        {
            Debug.LogError("找不到主角状态，请检查 PartyManager 的主角配置。");
            return;
        }

        if (hero.CurrentHP <= 0)
        {
            Debug.LogWarning("主角 HP 为 0，请先在背包中恢复 HP。");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(Constants.BATTLE_SCENE))
        {
            Debug.LogError("请把 BattleScene 加入构建场景列表并勾选。");
            return;
        }

        currentLocationId = Constants.LOCATION_RPG;
        hasPendingAction = true;

        SceneManager.LoadScene(Constants.BATTLE_SCENE);
    }

    // rewardMoney、rewardExp：本次战斗累计的奖励，只有完成时才发放。
    public bool FinishBattle(
        BattleResult result,
        int rewardMoney = 0,
        int rewardExp = 0)
    {
        // 只有正在进行的 RPG 行动才能结算。
        // 第一次结算后，这个条件也会阻止重复调用。
        if (currentLocationId != Constants.LOCATION_RPG || !hasPendingAction)
            return false;

        if (!Application.CanStreamedLevelBeLoaded(Constants.MAP_SCENE))
        {
            Debug.LogError("找不到 MapScene，请检查构建场景列表。");
            return false;
        }

        PartyManager party = PartyManager.Instance;
        GameTimeManager timeManager = GameTimeManager.Instance;

        if (party == null || timeManager == null)
        {
            Debug.LogError("缺少队伍或时间管理器，无法结算战斗。");
            return false;
        }

        PartyMemberState hero =
            party.GetMemberState(party.MainCharacterId);

        if (hero == null || hero.MaxHP <= 0)
        {
            Debug.LogError("主角状态无效，无法结算战斗。");
            return false;
        }

        // 零血时即使有人请求撤退，也必须按失败处理。
        if (hero.CurrentHP <= 0)
            result = BattleResult.Defeated;

        switch (result)
        {
            case BattleResult.Completed:
                // 奖励只在完成时发放；撤退和失败都作废。
                if (rewardMoney > 0)
                    party.AddMoney(rewardMoney);

                // EXP 只发给主角。
                if (rewardExp > 0)
                    party.AddEXP(party.MainCharacterId, rewardExp);

                // 复用已有方法：清除待结算标记、
                // 报告地点行动、推进一个时段。
                CompleteCurrentAction();

                Debug.Log(
                    $"战斗奖励：金钱 +{rewardMoney}，EXP +{rewardExp}。"
                    + $"现在金钱 {party.Money}，主角 EXP {hero.EXP}");
                break;

            case BattleResult.Retreated:
                hasPendingAction = false;
                break;

            case BattleResult.Defeated:
                // 先关闭结算入口，再修改状态和时间。
                hasPendingAction = false;

                int recoveryHP = Mathf.Clamp(
                    Mathf.CeilToInt(hero.MaxHP * 0.25f),
                    1,
                    hero.MaxHP
                );

                // 死亡和掉出场地都统一设为 25% HP。
                if (hero.CurrentHP < recoveryHP)
                {
                    party.RestoreHP(
                        party.MainCharacterId,
                        recoveryHP - hero.CurrentHP
                    );
                }
                else if (hero.CurrentHP > recoveryHP)
                {
                    party.TakeDamage(
                        party.MainCharacterId,
                        hero.CurrentHP - recoveryHP
                    );
                }

                // 失败消耗时间，但不报告任务完成。
                timeManager.AdvanceTime();
                break;

            default:
                Debug.LogError("未知的战斗结算结果。");
                return false;
        }

        // 不论哪种结果，回地图时主角 SP 回满。
        party.RestoreSP(
            party.MainCharacterId,
            hero.MaxSP - hero.CurrentSP);

        currentLocationId = Constants.LOCATION_MAP;

        Debug.Log(
            $"战斗结束：{result}，返回地图。主角 HP："
            + $"{hero.CurrentHP}/{hero.MaxHP}");

        // 第一次通关：先播通关后剧情，播完再回地图。
        // 此时行动已经结算，剧情结束时不会再推进时间。
        if (result == BattleResult.Completed &&
            !afterBattleStoryPlayed &&
            !string.IsNullOrWhiteSpace(afterBattleStoryFile) &&
            GameManager.Instance != null)
        {
            afterBattleStoryPlayed = true;

            GameManager gm = GameManager.Instance;
            gm.PrepareStory(afterBattleStoryFile.Trim());
            gm.storyReturnScene = Constants.MAP_SCENE;

            SceneManager.LoadScene(Constants.GAME_SCENE);
            return true;
        }

        SceneManager.LoadScene(Constants.MAP_SCENE);
        return true;
    }
}