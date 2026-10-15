using UnityEngine;
using UnityEngine.InputSystem;

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
    }

    private void Update()
    {
        if (isReturning || !enableTestReturn)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
        {
            ReturnToMapForTesting();
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
        if (isReturning)
            return;

        WorldManager world = WorldManager.Instance;

        if (world == null)
        {
            Debug.LogWarning(
                "当前为单独场景测试，无法返回完整地图流程。请停止运行，"
                + "从正式入口启动后再测试神社往返。",
                this);

            return;
        }

        isReturning = world.ReturnFromBattleForTesting();

        if (!isReturning)
        {
            Debug.LogWarning(
                "返回未执行：请确认本次是通过地图神社进入的战斗。",
                this);
        }
    }
}