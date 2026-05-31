using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HistoryManager : MonoBehaviour
{
    public Transform historyContent; // ScrollView的Content对象
    public GameObject historyItemPrefab; // 用于显示历史记录的文本预制件
    public GameObject historyScrollView; // ScrollView对象
    public Button closeButton; // 关闭按钮

    private LinkedList<VNManager.historyData> historyRecords; // 保存历史记录

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
        historyScrollView.SetActive(false);//启动时隐藏历史界面
        closeButton.onClick.AddListener(CloseHistory);//绑定关闭按钮事件
    }
    // 显示历史记录
    public void ShowHistory(LinkedList<VNManager.historyData> records)
    {

        closeButton.GetComponentInChildren<TextMeshProUGUI>().text = GetLocalized(Constants.CLOSE);
        // 清空现有的历史记录
        foreach (Transform child in historyContent)
        {
            Destroy(child.gameObject);
        }
        historyRecords = records;
        LinkedListNode<VNManager.historyData> currentNode = historyRecords.Last;
        while (currentNode != null)
        {
            var name = currentNode.Value.chineseName;
            var content = currentNode.Value.chineseContent;
            switch (MenuManager.Instance.currentLanguageIndex)
            {
                case 0:
                    break;
                case 1:
                    name = currentNode.Value.englishName;
                    content = currentNode.Value.englishContent;
                    break;
                case 2:
                    name = currentNode.Value.japaneseName;
                    content = currentNode.Value.japaneseContent;
                    break;
            }

            AddHistoryItem(name + GetLocalized(Constants.COLON) + content); // 添加历史记录项
            currentNode = currentNode.Previous;
        } // 移动到前一个节点
        historyContent.GetComponent<RectTransform>().localPosition = Vector3.zero; // 将滚动视图的位置重置为顶部
        historyScrollView.SetActive(true); // 显示历史记录界面
    }

    // 关闭历史记录
    public void CloseHistory()
    {
        historyScrollView.SetActive(false); // 隐藏历史记录界面
    }

    // 添加历史记录项
    private void AddHistoryItem(string text)
    {
        GameObject historyItem = Instantiate(historyItemPrefab, historyContent);
        historyItem.GetComponentInChildren<TextMeshProUGUI>().text = text;
        historyItem.transform.SetAsFirstSibling(); // 将历史记录项放在顶部
    }
    string GetLocalized(string key)
    {
        return LocalizationManager.Instance.GetLocalizedValue(key);
    }
}