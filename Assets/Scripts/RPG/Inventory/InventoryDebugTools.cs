using UnityEngine;

public class InventoryDebugTools : MonoBehaviour
{
    [ContextMenu("背包测试/1 打印背包")]
    private void PrintInventory()
    {
        var inventory = GetInventory();
        if (inventory == null) return;

        foreach (var data in inventory.AllItems)
        {
            Debug.Log(
                $"[背包] {data.itemName}（{data.itemId}）" +
                $"数量={inventory.GetCount(data.itemId)}",
                this);
        }
    }

    [ContextMenu("背包测试/2 模拟一组背包操作")]
    private void SimulateInventory()
    {
        var inventory = GetInventory();
        var player = PlayerStateManager.Instance;

        if (inventory == null || player == null)
            return;

        // 恢复药：3 → 5。
        inventory.AddItem("hp_potion", 2);

        // HP：100 → 60 → 80；恢复药：5 → 4。
        player.TakeDamage(40);
        bool usedPotion = inventory.TryUseItem("hp_potion");

        Debug.Log(
            $"[使用药水后] 成功={usedPotion}，" +
            $"HP={player.CurrentHP}/{player.MaxHP}，" +
            $"恢复药={inventory.GetCount("hp_potion")}",
            this);

        // 武器不能当作消耗品使用。
        bool usedWeapon = inventory.TryUseItem("training_sword");

        // 物品不足时不能扣除。
        bool removedTooMany =
            inventory.TryRemoveItem("hp_potion", 100);

        // 满血时使用恢复药，不消耗库存。
        player.RestoreHP(100);
        bool usedAtFullHP = inventory.TryUseItem("hp_potion");

        // 最后一把剑移除后，数量应为0。
        bool removedSword =
            inventory.TryRemoveItem("training_sword", 1);

        Debug.Log(
            $"[背包保护] 使用武器={usedWeapon}，" +
            $"扣除100瓶药={removedTooMany}，" +
            $"满血使用药={usedAtFullHP}，" +
            $"移除练习剑={removedSword}",
            this);

        PrintInventory();
    }

    private InventoryManager GetInventory()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请先进入 Play 模式。", this);
            return null;
        }

        var inventory = InventoryManager.Instance;

        if (inventory == null)
        {
            Debug.LogWarning(
                "未找到 InventoryManager，请从游戏原入口运行。",
                this);
        }

        return inventory;
    }
}