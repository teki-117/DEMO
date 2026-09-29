using UnityEngine;
using TMPro;

public class MapController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [Header("手机界面")]
    [SerializeField] private GameObject phoneRoot;
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

        WorldManager.Instance.EnterLocation(locationId);
    }
}