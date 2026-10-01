using UnityEngine;

// 任务的固定配置。每条任务创建一个资产，游玩进度放在 QuestState 中。
[CreateAssetMenu(fileName = "NewQuest", menuName = "RPG/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("任务信息")]
    public string questId;
    public string questName;

    [TextArea(2, 4)]
    public string description;

    [Header("任务目标")]
    public string targetLocationId = "School";

    [Min(1)]
    public int requiredCount = 1;

    [Header("任务奖励")]
    public string rewardCharacterId = "sakura";

    [Min(0)]
    public int rewardAffection = 5;
}
