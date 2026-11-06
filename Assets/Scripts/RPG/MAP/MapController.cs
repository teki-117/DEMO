using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MapController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [Header("手机界面")]
    [SerializeField] private GameObject phoneRoot;

    [Header("提示文字（可选）")]
    [Tooltip("点到还没解锁的地点时显示提示。")]
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private float hintDuration = 2f;

    private float hintHideTime;
    private bool isOpeningSave;

    private void Start()
    {
        // 记录当前所在的地图场景，供设置等界面返回使用。
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentScene = "MapScene";
        }

        if (WorldManager.Instance != null)
        {
            Debug.Log(
                $"地图已打开，当前地点：{WorldManager.Instance.CurrentLocationId}"
            );
        }
        if (timeText != null && GameTimeManager.Instance != null)
        {
            timeText.text = GameTimeManager.Instance.GetDisplayText();
        }

        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (hintText != null &&
            hintText.gameObject.activeSelf &&
            Time.unscaledTime >= hintHideTime)
        {
            hintText.gameObject.SetActive(false);
        }
    }

    public void SelectLocation(string locationId)
    {
        // 手机显示期间（包括滑入和滑出动画），不响应地图地点操作。
        if (phoneRoot != null && phoneRoot.activeInHierarchy)
        {
            return;
        }

        if (WorldManager.Instance == null)
        {
            Debug.LogError("地图中缺少 WorldManager，请检查挂载。");
            return;
        }

        if (!WorldManager.Instance.EnterLocation(locationId))
            ShowHint("这里现在还不能去。");
    }

    private void ShowHint(string message)
    {
        if (hintText == null)
            return;

        hintText.text = message;
        hintText.gameObject.SetActive(true);
        hintHideTime = Time.unscaledTime + hintDuration;
    }

    // 地图或手机里的“存档”按钮调用：截一张图，然后打开存档界面。
    // 存档按钮可以放在手机主页里，所以这里不检查手机是否打开。
    public void OpenSaveMenu()
    {
        if (isOpeningSave)
            return;

        GameManager gm = GameManager.Instance;

        if (gm == null || WorldManager.Instance == null)
        {
            Debug.LogError("存档需要 GameManager 和 WorldManager，请从正式入口启动。");
            return;
        }

        isOpeningSave = true;
        StartCoroutine(OpenSaveMenuRoutine(gm));
    }

    private IEnumerator OpenSaveMenuRoutine(GameManager gm)
    {
        // 等这一帧画完再截图。
        yield return new WaitForEndOfFrame();

        ScreenShotter shotter = GetComponent<ScreenShotter>();
        if (shotter == null)
            shotter = gameObject.AddComponent<ScreenShotter>();

        Texture2D shot = shotter.CaptureScreenshot();
        byte[] png = shot != null ? shot.EncodeToPNG() : null;

        if (shot != null)
            Destroy(shot);

        gm.pendingData = new SaveData
        {
            savedScene = "MapScene",
            savedScreenshotData = png,
            savedHistoryRecords = new LinkedList<ExcelData>(gm.historyRecords),
            savedPlayerName = gm.playerName,
            savedCharacters = new List<CharacterSaveData>(),
            savedAffection = CharacterStateManager.Instance != null
                ? CharacterStateManager.Instance.DumpStates()
                : null,
            savedStoryReturnScene = gm.storyReturnScene,
            rpg = WorldManager.Instance.CaptureRpgSave()
        };

        gm.currentScene = "MapScene";
        gm.currentSaveLoadMode = GameManager.SaveLoadMode.Save;
        SceneManager.LoadScene(Constants.SAVE_LOAD_SCENE);
    }
}
