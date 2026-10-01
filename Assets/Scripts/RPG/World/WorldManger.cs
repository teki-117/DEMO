using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldManager : MonoBehaviour
{
    private bool hasPendingAction;
    public static WorldManager Instance { get; private set; }

    [SerializeField] private string currentLocationId = "Map";

    public string CurrentLocationId => currentLocationId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 开始新游戏时重置世界状态。
    public void ResetWorld()
    {
        currentLocationId = "Map";
        hasPendingAction = false;
    }

    // 地图界面统一通过这个入口选择地点。
    public void EnterLocation(string locationId)
    {
        switch (locationId)
        {
            case "School":
                EnterSchool();
                break;

            case "Home":
            case "Shop":
            case "RPG":
                currentLocationId = locationId;
                Debug.Log($"当前地点：{currentLocationId}，地点内容待接入。");
                break;

            default:
                Debug.LogWarning($"未知地点：{locationId}");
                break;
        }
    }

    private void EnterSchool()
    {
        var gm = GameManager.Instance;

        if (gm == null)
        {
            Debug.LogError("找不到 GameManager，请从原来的游戏入口启动。");
            return;
        }

        currentLocationId = "School";

        gm.PrepareStory(Constants.DEFAULT_STORY_FILE);
        gm.storyReturnScene = "MapScene";
        hasPendingAction = true;

        SceneManager.LoadScene(Constants.GAME_SCENE);
    }
    public void CompleteCurrentAction()
    {
        // 没有待结算行动时，直接返回。
        if (!hasPendingAction)
        {
            return;
        }

        if (GameTimeManager.Instance == null)
        {
            Debug.LogError("缺少 GameTimeManager，无法结算行动时间。");
            return;
        }

        // 先标记为已结算，避免同一次行动重复结算。
        hasPendingAction = false;

        // 向任务系统报告一次完整的地点行动。
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ReportLocationActionCompleted(
                currentLocationId);
        }

        // 一次行动推进一个时间段。
        GameTimeManager.Instance.AdvanceTime();
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}