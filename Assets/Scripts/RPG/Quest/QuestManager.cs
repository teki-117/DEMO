using System;
using System.Collections.Generic;
using UnityEngine;

// 管理本局任务。剧情负责解锁，目标达成后自动完成和发奖。
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("本 Demo 使用的任务")]
    [SerializeField] private QuestData[] allQuestDatas = new QuestData[0];

    private readonly Dictionary<string, QuestData> definitions = new Dictionary<string, QuestData>();
    private readonly Dictionary<string, QuestState> states = new Dictionary<string, QuestState>();
    private readonly List<QuestData> quests = new List<QuestData>();
    private readonly HashSet<string> warnedPendingRewards = new HashSet<string>();

    private bool hasPendingRewards;
    private float nextRewardRetryTime;

    // 之后由手机任务页订阅，任务变化时刷新两个分组。
    public event Action OnQuestsChanged;

    // 保留 Inspector 中的任务排列顺序。
    public IReadOnlyList<QuestData> Quests => quests;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResetQuests();
    }

    private void Update()
    {
        // 角色系统暂未就绪时，自动补发奖励。这里不会推进游戏时间。
        if (!hasPendingRewards || Time.unscaledTime < nextRewardRetryTime)
            return;

        nextRewardRetryTime = Time.unscaledTime + 1f;

        if (TryGrantPendingRewards())
            OnQuestsChanged?.Invoke();
    }

    public void ResetQuests()
    {
        definitions.Clear();
        states.Clear();
        quests.Clear();
        warnedPendingRewards.Clear();
        hasPendingRewards = false;
        nextRewardRetryTime = 0f;

        if (allQuestDatas != null)
        {
            foreach (var data in allQuestDatas)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.questId))
                {
                    Debug.LogWarning("QuestManager：跳过空任务或缺少 ID 的任务。", this);
                    continue;
                }

                if (definitions.ContainsKey(data.questId))
                {
                    Debug.LogWarning($"QuestManager：任务 ID 重复，跳过 {data.questId}。", this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(data.targetLocationId) ||
                    data.requiredCount < 1 || data.rewardAffection < 0 ||
                    (data.rewardAffection > 0 && string.IsNullOrWhiteSpace(data.rewardCharacterId)))
                {
                    Debug.LogWarning($"QuestManager：任务 {data.questId} 的目标或奖励配置无效。", this);
                    continue;
                }

                definitions.Add(data.questId, data);
                states.Add(data.questId, new QuestState(data.questId));
                quests.Add(data);
            }
        }

        OnQuestsChanged?.Invoke();
    }

    public QuestData GetQuestData(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;
        return definitions.TryGetValue(questId, out var data) ? data : null;
    }

    public QuestState GetState(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;
        return states.TryGetValue(questId, out var state) ? state : null;
    }

    // 后续在指定剧情触发点调用。重复触发不会重置进度或重复解锁。
    public bool UnlockQuest(string questId)
    {
        var state = GetState(questId);
        if (state == null || state.status != QuestStatus.Locked)
            return false;

        state.status = QuestStatus.InProgress;
        OnQuestsChanged?.Invoke();
        return true;
    }

    // 每次调用代表一次有效的地点行动结算。
    // 正式接入学校剧情时，由行动结算入口报告，避免单纯进地图就完成任务。
    public void ReportLocationActionCompleted(string locationId)
    {
        if (string.IsNullOrWhiteSpace(locationId)) return;

        bool changed = false;

        foreach (var data in quests)
        {
            var state = states[data.questId];
            if (state.status != QuestStatus.InProgress ||
                data.targetLocationId != locationId)
                continue;

            state.progress = Mathf.Min(state.progress + 1, data.requiredCount);
            changed = true;

            if (state.progress >= data.requiredCount)
            {
                state.status = QuestStatus.Completed;
                hasPendingRewards = true;
            }
        }

        if (hasPendingRewards)
        {
            nextRewardRetryTime = Time.unscaledTime + 1f;
            if (TryGrantPendingRewards())
                changed = true;
        }

        if (changed)
            OnQuestsChanged?.Invoke();
    }

    private bool TryGrantPendingRewards()
    {
        bool grantedAny = false;
        hasPendingRewards = false;

        foreach (var data in quests)
        {
            var state = states[data.questId];
            if (state.status != QuestStatus.Completed || state.rewardGranted)
                continue;

            if (TryGrantReward(data, state))
                grantedAny = true;
            else
                hasPendingRewards = true;
        }

        return grantedAny;
    }

    private bool TryGrantReward(QuestData data, QuestState state)
    {
        if (state.status != QuestStatus.Completed || state.rewardGranted)
            return false;

        var characterManager = CharacterStateManager.Instance;

        if (data.rewardAffection > 0 &&
            (characterManager == null ||
             !characterManager.DumpStates().ContainsKey(data.rewardCharacterId)))
        {
            // 同一任务只提示一次，之后继续自动重试。
            if (warnedPendingRewards.Add(data.questId))
            {
                Debug.LogWarning($"QuestManager：任务 {data.questId} 已完成，奖励角色 {data.rewardCharacterId} 尚未就绪；就绪后会自动发奖。", this);
            }

            return false;
        }

        // 发奖前记录标记，防止重复行动或剧情触发导致重复奖励。
        state.rewardGranted = true;
        warnedPendingRewards.Remove(data.questId);

        if (data.rewardAffection > 0)
            characterManager.ChangeAffection(data.rewardCharacterId, data.rewardAffection);

        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
