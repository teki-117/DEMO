using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class BattleSceneController : MonoBehaviour
{
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

    private BattlePlayerController playerController;

    private void Start()
    {
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
            exitArea == null ||
            objectiveText == null)
        {
            Debug.LogError(
                "请检查主角控制脚本、Boss Room Area、Exit Area 和 Objective Text。",
                this);

            enabled = false;
        }
    }

    private void Update()
    {
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

        Vector2 playerPosition = player.position;

        bool insideBossRoom =
            bossRoomArea.OverlapPoint(playerPosition);

        bool insideExit =
            exitArea.OverlapPoint(playerPosition);

        if (insideBossRoom && insideExit)
        {
            objectiveText.text =
                "E: Complete demo    Esc: Retreat";
        }
        else if (insideBossRoom)
        {
            objectiveText.text =
                "Boss room (placeholder) - Reach the exit";
        }
        else
        {
            objectiveText.text =
                "Reach the boss room on the right    Esc: Retreat";
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
                statusText.text = "HP -- / --    ATK --    DEF --";

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
                $"ATK {hero.Attack}    DEF {hero.Defense}";

            if (hero.CurrentHP <= 0)
            {
                statusText.text += "    DOWN";
            }
        }
    }
    private void EndBattle(BattleResult result)
    {
        if (isReturning)
            return;

        WorldManager world = WorldManager.Instance;

        if (world == null)
        {
            Debug.LogError(
                "缺少 WorldManager，请停止运行，"
                + "从正式入口进入地图，再通过神社测试。",
                this);

            // 避免独立场景测试时每帧重复报错。
            enabled = false;
            return;
        }

        isReturning = world.FinishBattle(result);

        if (!isReturning)
        {
            Debug.LogError(
                "战斗结算未执行，请检查前面的 Console 提示。",
                this);

            enabled = false;
        }
    }
}