using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HistoryManager : MonoBehaviour
{
    public Transform historyContent; // ScrollView的Content对象
    public GameObject historyItemPrefab; // 用于显示历史记录的文本预制件
    public GameObject historyScrollView; // ScrollView对象
    public Button closeButton; // 关闭按钮

    private LinkedList<ExcelReader.ExcelData> historyRecords; // 保存历史记录

    public static HistoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        closeButton.onClick.AddListener(CloseHistory);

        if (GameManager.Instance.historyRecords != null && GameManager.Instance.historyRecords.Count > 0)
        {
            ShowHistory(GameManager.Instance.historyRecords);
        }
        else
        {
            historyScrollView.SetActive(true);
            Debug.LogWarning("No history records found.");
        }
    }
    // 显示历史记录
    public void ShowHistory(LinkedList<ExcelReader.ExcelData> records)
    {
        // 清空现有的历史记录
        foreach (Transform child in historyContent)
        {
            Destroy(child.gameObject);
        }
        historyRecords = records;
        LinkedListNode<ExcelReader.ExcelData> currentNode = historyRecords.Last;
        while (currentNode != null)
        {
            var name = LM.GetSpeakerName(currentNode.Value);      // 获取说话者名字
            var content = LM.GetSpeakingContent(currentNode.Value); // 获取说话内容
            AddHistoryItem(name + LM.GLV(Constants.COLON) + content); // 添加历史记录项
            currentNode = currentNode.Previous; // 移动到前一个节点
        }
        historyContent.GetComponent<RectTransform>().localPosition = Vector3.zero; // 将滚动视图的位置重置为顶部
        historyScrollView.SetActive(true); // 显示历史记录界面
    }
    // 关闭历史记录
    public void CloseHistory()
    {
        GameManager.Instance.historyRecords.RemoveLast(); // 移除最后一个历史记录
        SceneManager.LoadScene(GameManager.Instance.currentScene); // 返回到当前场景
    }

    // 添加历史记录项
    private void AddHistoryItem(string text)
    {
        GameObject historyItem = Instantiate(historyItemPrefab, historyContent);
        historyItem.GetComponentInChildren<TextMeshProUGUI>().text = text;
        historyItem.transform.SetAsFirstSibling(); // 将历史记录项放在顶部
    }
    
}