# DEMO V2 — 项目交接（给 Claude 读）

个人 Unity Demo：原 Galgame/VN 工程升级成日式 RPG。日常行动参考《女神异闻录3 Reload》，战斗是《空洞骑士》《丝之歌》式 2D 横版即时动作。
Unity 6000.4.4f1 · URP · Input System（只用键盘）· TextMeshPro · DOTween · Live2D。
仓库：teki-117/DEMO（main）。总控文档：https://claude.ai/artifact/Asupu2ggFYq2Ca4K7T3Sp4

## 合作规则（必须遵守）

- 不做“边做边学”的分步教学：直接给完整的一块功能，我在 Unity 里挂载、测试。我不懂的会问，或者最后再一起说。
- 有疑问先问我，不要猜着做。
- 脚本按大功能分类：同一类功能全放进同一个脚本（例如玩家相关的都放 BattlePlayerController），不设行数上限，不因为文件长就拆。只有出现一类新的大功能时才新建脚本，新建前先说明理由。
- 每次开工（每周/每个阶段）前，先列出要做什么和具体思路给我确认，我提完意见再写代码。
- 每次交付写清：改了哪些文件、Inspector 要配什么、怎么验收。
- 所有 .cs 一律 UTF-8 保存。
- 功能全部完成后再一起做统一优化，现在不重构。
- 截止：1–2 周内完成（目标 2026-11-19 前）。

## 架构规则（沿用）

- Manager 保存状态并执行业务，UI 只读状态；不把新系统塞进 GameManager。
- 一次行动只结算一次时间（WorldManager.hasPendingAction）；看手机、换装、用药不推进时间。
- PartyManager 是 HP/SP、金钱、装备的唯一来源；战斗不复制主角状态，敌人 HP 只在场景内。
- 正式运行要从 MenuScene 启动（Manager 在前面的场景里 DontDestroyOnLoad）。
- 战斗结算（WorldManager.FinishBattle）：完成 = 时段+1 并报告任务；撤退 = 不耗时；失败 = HP 设为 25% 且时段+1；HP 为 0 不能进关。

## 脚本文件夹（Assets/Scripts，11-11 整理后）

- Core：GameManager、Constants、存档数据、音频、本地化、Excel 读取
- VN：VNManager（已合成一个文件）、选项、角色显示、菜单/设置/存读档/画廊等界面
- RPG：World、时间、队伍、背包、任务的 Manager 和数据
- RPG/UI：地图、手机各页面、商店、任务界面
- Battle：4 个战斗控制器
- Editor：BattleArtImporter
- 挂在物体或 Prefab 上的脚本必须一个文件一个类，不能合并；移动脚本要连 .meta 一起移。

## 战斗代码位置（Assets/Scripts/Battle）

- BattleSceneController：相机、HUD、目标提示、Boss 房与出口、结束入口、奖励累计
- BattlePlayerController：移动、跳跃、攻击、受伤、技能
- BattleEnemyController：小怪 AI、HP、受击
- BattleBossController：Boss（第10周新建，已确认单独一个文件）

## 已确认的设计

- 技能 3 个：近战冲刺斩、法术（耗 SP）、回血（耗 SP），扣 SP 用 PartyManager.TrySpendSP
- 战斗奖励：金钱 + EXP；掉落物品 = Boss 房钥匙；再次进关敌人重生
- 战斗文字接三语本地化：LM.GLV(key) + StreamingAssets 里的语言 JSON
- 不需要手柄

## 下一步（第一批）

1. 42 个 GBK 编码的 .cs 转 UTF-8，只改编码不改内容；加 .editorconfig（charset = utf-8）。
2. 删除 Assets/_Recovery（含 .meta），把 _Recovery 加进 .gitignore。
3. 修战斗不结算：BattleScene 里 BattleSceneController 的 bossRoomArea、exitArea、objectiveText 是空的，场景里也没有这三个物体，所以 Start 里报错并 enabled=false，导致 Esc/E/HP归零/掉落都不结算、相机也不跟随。
   - 代码：objectiveText 缺失只警告、不停用。
   - 场景（我在 Unity 里做）：建 BossRoomArea、ExitArea（BoxCollider2D，勾 Is Trigger）和 BattleHUD 下的 ObjectiveText，拖进 Inspector。
4. 我验收第7周全部项目后，进入第8周：敌人前摇、击退、3 个技能、奖励与结算画面。
