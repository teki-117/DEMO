using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class BattleSceneController : MonoBehaviour
{
    // 当前战斗场景的控制器，敌人被击败时向它报告奖励。
    public static BattleSceneController Current { get; private set; }

    [Header("场景引用")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera battleCamera;

    [Header("相机水平跟随")]
    [Min(0.01f)]
    [SerializeField] private float followSmoothTime = 0.15f;

    [SerializeField] private float horizontalOffset = 0f;

    [Header("开发测试")]
    [SerializeField] private bool enableTestReturn = true;

    private float cameraVelocityX;
    private float fixedCameraY;
    private float fixedCameraZ;

    private bool isReturning;

    [Header("战斗 HUD")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private TMP_Text statusText;

    private PartyManager subscribedParty;

    [Header("关卡区域")]
    [SerializeField] private BoxCollider2D bossRoomArea;
    [SerializeField] private BoxCollider2D exitArea;
    [SerializeField] private TMP_Text objectiveText;

    [Header("掉落失败")]
    [SerializeField] private float fallFailY = -8f;

    [Header("Boss 房钥匙与门")]
    [Tooltip("钥匙物体：场景里先放好，开始时自动隐藏，掉钥匙的敌人被击败时出现在它的位置。")]
    [SerializeField] private GameObject keyPickup;

    [Tooltip("主角离钥匙多近算捡到。")]
    [Min(0.1f)]
    [SerializeField] private float keyPickupRadius = 0.9f;

    [Tooltip("Boss 房入口的门：捡到钥匙后隐藏。留空表示不需要钥匙。")]
    [SerializeField] private GameObject bossDoor;

    [Header("结算画面")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    private BattlePlayerController playerController;

    // 本次战斗累计的奖励，结算时交给 WorldManager 发放。
    private int earnedMoney;
    private int earnedExp;
    private int defeatedCount;
    private bool hasKey;

    private bool isShowingResult;
    private BattleResult pendingResult;
    private int resultShownFrame;

    private void Awake()
    {
        Current = this;
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;

        // 防止结算画面中途离开场景时游戏一直暂停。
        if (isShowingResult)
            Time.timeScale = 1f;
    }

    private void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (keyPickup != null)
            keyPickup.SetActive(false);

        // 没配门就当作 Boss 房不需要钥匙。
        hasKey = bossDoor == null;

        if (player == null || battleCamera == null)
        {
            Debug.LogError(
                "请给 BattleSceneController 配置 Player 和 Battle Camera。",
                this);

            enabled = false;
            return;
        }

        // 保留相机原来的高度与深度，只跟随水平方向。
        Vector3 position = battleCamera.transform.position;
        fixedCameraY = position.y;
        fixedCameraZ = position.z;

        // 开场直接对准主角，避免从远处滑过来。
        battleCamera.transform.position = new Vector3(
            player.position.x + horizontalOffset,
            fixedCameraY,
            fixedCameraZ
        );

        playerController = player.GetComponent<BattlePlayerController>();

        if (playerController == null ||
            bossRoomArea == null ||
            exitArea == null)
        {
            Debug.LogError(
                "请检查主角控制脚本、Boss Room Area 和 Exit Area。",
                this);

            enabled = false;
            return;
        }

        // 提示文字只是显示用，缺了不影响结算。
        if (objectiveText == null)
        {
            Debug.LogWarning(
                "BattleSceneController 没有配置 Objective Text，目标提示不会显示。",
                this);
        }

        if (resultPanel == null || resultText == null)
        {
            Debug.LogWarning(
                "BattleSceneController 没有配置结算画面，结束时会直接返回地图。",
                this);
        }

        if (bossDoor != null && keyPickup == null)
        {
            Debug.LogWarning(
                "配置了 Boss Door 但没有配置 Key Pickup，门将无法打开。",
                this);
        }
    }

    private void SetObjective(string text)
    {
        if (objectiveText != null)
            objectiveText.text = text;
    }

    private void Update()
    {
        // 结算画面显示时游戏暂停，只等待确认键。
        if (isShowingResult)
        {
            UpdateResultScreen();
            return;
        }

        if (isReturning ||
            player == null ||
            playerController == null ||
            Time.timeScale <= 0f)
        {
            return;
        }

        // 先判失败，避免零血时按撤退绕过失败结算。
        if (playerController.IsDefeated ||
            player.position.y < fallFailY)
        {
            EndBattle(BattleResult.Defeated);
            return;
        }

        UpdateKeyPickup();

        Vector2 playerPosition = player.position;

        bool insideBossRoom =
            bossRoomArea.OverlapPoint(playerPosition);

        bool insideExit =
            exitArea.OverlapPoint(playerPosition);

        if (insideBossRoom && insideExit)
        {
            SetObjective("E：完成关卡    Esc：撤退");
        }
        else if (insideBossRoom)
        {
            SetObjective("Boss 房（占位）——前往出口");
        }
        else if (!hasKey)
        {
            SetObjective("击败敌人，找到 Boss 房钥匙    Esc：撤退");
        }
        else
        {
            SetObjective("前往右边的 Boss 房    Esc：撤退");
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            EndBattle(BattleResult.Retreated);
            return;
        }

        // 保留旧测试快捷键，统一走撤退规则。
        if (enableTestReturn && keyboard.f1Key.wasPressedThisFrame)
        {
            ReturnToMapForTesting();
            return;
        }

        if (insideBossRoom &&
            insideExit &&
            keyboard.eKey.wasPressedThisFrame)
        {
            EndBattle(BattleResult.Completed);
        }
    }

    private void LateUpdate()
    {
        if (isReturning || player == null || battleCamera == null)
            return;

        // 暂停时不更新平滑计算。
        if (Time.deltaTime <= 0f)
            return;

        float targetX = player.position.x + horizontalOffset;

        float nextX = Mathf.SmoothDamp(
            battleCamera.transform.position.x,
            targetX,
            ref cameraVelocityX,
            followSmoothTime
        );

        battleCamera.transform.position = new Vector3(
            nextX,
            fixedCameraY,
            fixedCameraZ
        );
    }

    public void ReturnToMapForTesting()
    {
        EndBattle(BattleResult.Retreated);
    }

    // ---------- 奖励与钥匙 ----------

    public void ReportEnemyDefeated(BattleEnemyController enemy)
    {
        if (enemy == null || isShowingResult || isReturning)
            return;

        defeatedCount++;
        earnedMoney += enemy.MoneyReward;
        earnedExp += enemy.ExpReward;

        if (enemy.DropsKey && !hasKey && keyPickup != null)
        {
            Vector3 dropPosition = enemy.transform.position;
            dropPosition.z = keyPickup.transform.position.z;

            keyPickup.transform.position = dropPosition;
            keyPickup.SetActive(true);
        }
    }

    private void UpdateKeyPickup()
    {
        if (hasKey || keyPickup == null || !keyPickup.activeSelf)
            return;

        float distance = Vector2.Distance(
            keyPickup.transform.position, player.position);

        if (distance > keyPickupRadius)
            return;

        hasKey = true;
        keyPickup.SetActive(false);

        if (bossDoor != null)
            bossDoor.SetActive(false);

        Debug.Log("捡到 Boss 房钥匙，入口的门已打开。", this);
    }

    // ---------- HUD ----------

    private void OnEnable()
    {
        subscribedParty = PartyManager.Instance;

        if (subscribedParty != null)
        {
            subscribedParty.OnStateChanged += RefreshHUD;
        }

        RefreshHUD();
    }

    private void OnDisable()
    {
        if (subscribedParty != null)
        {
            subscribedParty.OnStateChanged -= RefreshHUD;
        }

        subscribedParty = null;
    }

    private void RefreshHUD()
    {
        PartyMemberState hero = subscribedParty != null
            ? subscribedParty.GetMemberState(subscribedParty.MainCharacterId)
            : null;

        if (hero == null)
        {
            if (hpBar != null)
                hpBar.SetValueWithoutNotify(0f);

            if (statusText != null)
                statusText.text = "HP -- / --    SP -- / --    ATK --    DEF --";

            return;
        }

        float hpRatio = hero.MaxHP > 0
            ? (float)hero.CurrentHP / hero.MaxHP
            : 0f;

        if (hpBar != null)
        {
            hpBar.SetValueWithoutNotify(Mathf.Clamp01(hpRatio));
        }

        if (statusText != null)
        {
            statusText.text =
                $"HP {hero.CurrentHP} / {hero.MaxHP}    " +
                $"SP {hero.CurrentSP} / {hero.MaxSP}    " +
                $"ATK {hero.Attack}    DEF {hero.Defense}";

            if (hero.CurrentHP <= 0)
            {
                statusText.text += "    DOWN";
            }
        }
    }

    // ---------- 结束与结算画面 ----------

    private void EndBattle(BattleResult result)
    {
        if (isReturning || isShowingResult)
            return;

        if (WorldManager.Instance == null)
        {
            Debug.LogError(
                "缺少 WorldManager，请停止运行，"
                + "从正式入口进入地图，再通过神社测试。",
                this);

            // 避免独立场景测试时每帧重复报错。
            enabled = false;
            return;
        }

        // 和 WorldManager 的规则一致：零血时一律按失败显示。
        PartyManager party = PartyManager.Instance;
        PartyMemberState hero = party != null
            ? party.GetMemberState(party.MainCharacterId)
            : null;

        if (hero != null && hero.CurrentHP <= 0)
            result = BattleResult.Defeated;

        pendingResult = result;

        if (resultPanel == null || resultText == null)
        {
            ConfirmResult();
            return;
        }

        resultText.text = BuildResultText(result);
        resultPanel.SetActive(true);

        isShowingResult = true;
        resultShownFrame = Time.frameCount;

        // 暂停敌人和主角，等玩家看完再返回。
        Time.timeScale = 0f;
    }

    private string BuildResultText(BattleResult result)
    {
        string rewards =
            $"击败敌人：{defeatedCount}\n" +
            $"金钱：{earnedMoney}\n" +
            $"EXP：{earnedExp}";

        switch (result)
        {
            case BattleResult.Completed:
                return "关卡完成\n\n" +
                       $"击败敌人：{defeatedCount}\n" +
                       $"金钱 +{earnedMoney}\n" +
                       $"EXP +{earnedExp}\n\n" +
                       "SP 已回满，时段 +1\n\n" +
                       "按 E 返回地图";

            case BattleResult.Retreated:
                return "已撤退\n\n" +
                       rewards + "\n" +
                       "奖励未获得。SP 已回满，时段不变\n\n" +
                       "按 E 返回地图";

            default:
                return "战斗失败\n\n" +
                       rewards + "\n" +
                       "奖励未获得。HP 降为 25%，SP 已回满，时段 +1\n\n" +
                       "按 E 返回地图";
        }
    }

    private void UpdateResultScreen()
    {
        Keyboard keyboard = Keyboard.current;

        // 跳过打开画面的那一帧，避免出口的 E 直接把画面关掉。
        if (keyboard == null || Time.frameCount <= resultShownFrame)
            return;

        if (keyboard.eKey.wasPressedThisFrame)
            ConfirmResult();
    }

    private void ConfirmResult()
    {
        if (isReturning)
            return;

        isShowingResult = false;
        Time.timeScale = 1f;

        WorldManager world = WorldManager.Instance;

        if (world == null)
        {
            Debug.LogError("缺少 WorldManager，无法结算战斗。", this);
            enabled = false;
            return;
        }

        isReturning = world.FinishBattle(
            pendingResult, earnedMoney, earnedExp);

        if (!isReturning)
        {
            Debug.LogError(
                "战斗结算未执行，请检查前面的 Console 提示。",
                this);

            if (resultPanel != null)
                resultPanel.SetActive(false);

            enabled = false;
        }
    }
}
