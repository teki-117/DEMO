using UnityEngine;

public partial class VNManager
{
    private void Flow_UnlockQuest(string rawQuestId)
    {
        string questId = rawQuestId?.Trim();

        if (string.IsNullOrEmpty(questId))
        {
            Debug.LogError("剧情任务指令缺少任务 ID。", this);
            return;
        }

        var manager = QuestManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "剧情无法解锁任务：找不到 QuestManager。",
                this);
            return;
        }

        QuestData data = manager.GetQuestData(questId);

        if (data == null)
        {
            Debug.LogError(
                $"剧情中的任务 ID 没有注册：{questId}",
                this);
            return;
        }

        if (manager.UnlockQuest(questId))
        {
            Debug.Log(
                $"剧情解锁任务：{data.questName}（{questId}）",
                this);
        }
    }
}