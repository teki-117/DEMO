using UnityEngine;

// 通过 Inspector 模拟剧情事件；正式游戏里没有接取或领奖按钮。
public class QuestDebugTools : MonoBehaviour
{
    [SerializeField] private QuestData testQuest;

    [ContextMenu("任务测试/1 打印状态")]
    private void PrintState()
    {
        if (!TryGetContext(out var manager)) return;

        var state = manager.GetState(testQuest.questId);
        string rewardState = state.rewardGranted ? "是" : "否";
        string affection = "角色状态未就绪";
        var characterManager = CharacterStateManager.Instance;

        if (characterManager != null &&
            !string.IsNullOrWhiteSpace(testQuest.rewardCharacterId) &&
            characterManager.DumpStates().ContainsKey(testQuest.rewardCharacterId))
        {
            affection = characterManager.GetAffection(testQuest.rewardCharacterId).ToString();
        }

        Debug.Log($"[任务测试] {testQuest.questName} | 状态={state.status} | 进度={state.progress}/{testQuest.requiredCount} | 奖励已发放={rewardState} | {testQuest.rewardCharacterId}好感度={affection}", this);
    }

    [ContextMenu("任务测试/2 模拟剧情解锁")]
    private void Unlock()
    {
        if (!TryGetContext(out var manager)) return;

        Debug.Log(manager.UnlockQuest(testQuest.questId)
            ? "[任务测试] 剧情解锁成功，任务已开始追踪。"
            : "[任务测试] 未再次解锁：任务已经进行中或已完成。", this);
        PrintState();
    }

    [ContextMenu("任务测试/3 模拟一次目标地点行动")]
    private void ReportAction()
    {
        if (!TryGetContext(out var manager)) return;
        manager.ReportLocationActionCompleted(testQuest.targetLocationId);
        PrintState();
    }

    private bool TryGetContext(out QuestManager manager)
    {
        manager = QuestManager.Instance;

        if (!Application.isPlaying)
        {
            Debug.LogWarning("任务测试：请先进入 Play 模式。", this);
            return false;
        }

        if (manager == null || testQuest == null)
        {
            Debug.LogWarning("任务测试：请检查 QuestManager 和 Test Quest 引用。", this);
            return false;
        }

        if (manager.GetState(testQuest.questId) == null)
        {
            Debug.LogWarning("任务测试：请把 Test Quest 同时加入 QuestManager 的 All Quest Datas。", this);
            return false;
        }

        return true;
    }
}
