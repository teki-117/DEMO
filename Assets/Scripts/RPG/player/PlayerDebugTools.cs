using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDebugTools : MonoBehaviour
{
    [ContextMenu("玩家测试/1 打印当前状态")]
    private void PrintState()
    {
        var manager = GetManager();
        if (manager == null) return;

        Debug.Log(
            $"[玩家状态] 等级={manager.Level} EXP={manager.EXP} | " +
            $"HP={manager.CurrentHP}/{manager.MaxHP} | " +
            $"SP={manager.CurrentSP}/{manager.MaxSP} | " +
            $"攻击={manager.Attack} 防御={manager.Defense} | " +
            $"金钱={manager.Money}",
            this);
    }

    [ContextMenu("玩家测试/2 模拟一组状态变化")]
    private void SimulateChanges()
    {
        var manager = GetManager();
        if (manager == null) return;

        manager.TakeDamage(40);
        manager.RestoreHP(20);

        manager.TrySpendSP(5);
        manager.RestoreSP(2);

        manager.AddMoney(100);

        bool paid30 = manager.TrySpendMoney(30);
        bool paid1000 = manager.TrySpendMoney(1000);
        bool spent1000SP = manager.TrySpendSP(1000);

        Debug.Log(
            $"[玩家测试] 支付30={paid30}，" +
            $"支付1000={paid1000}，" +
            $"消耗1000SP={spent1000SP}",
            this);

        PrintState();
    }

    [ContextMenu("玩家测试/3 返回菜单测试新游戏")]
    private void ReturnToMenu()
    {
        if (GetManager() == null) return;

        SceneManager.LoadScene(Constants.MENU_SCENE);
    }

    private PlayerStateManager GetManager()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请先进入 Play 模式。", this);
            return null;
        }

        var manager = PlayerStateManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning(
                "未找到 PlayerStateManager，请从游戏原入口运行。",
                this);
        }

        return manager;
    }
}