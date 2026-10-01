using System;

// 任务由剧情解锁，完成目标后直接进入已完成。
public enum QuestStatus
{
    Locked = 0,        // 未解锁：手机任务页隐藏
    InProgress = 1,    // 进行中：放在未完成分组
    Completed = 2      // 已完成：放在已完成分组
}

[Serializable]
public class QuestState
{
    public string questId;
    public QuestStatus status;
    public int progress;
    public bool rewardGranted;

    public QuestState(string questId)
    {
        this.questId = questId;
        status = QuestStatus.Locked;
        progress = 0;
        rewardGranted = false;
    }
}
