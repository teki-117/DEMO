using UnityEngine;

public class QuestPageUI : MonoBehaviour
{
    [Header("分类标题")]
    [SerializeField] private QuestSectionUI unfinishedSection;
    [SerializeField] private QuestSectionUI completedSection;

    [Header("任务列表容器")]
    [SerializeField] private Transform unfinishedList;
    [SerializeField] private Transform completedList;

    [Header("任务条目预制体")]
    [SerializeField] private QuestItemUI itemPrefab;

    private QuestManager manager;

    private void OnEnable()
    {
        if (unfinishedSection == null ||
            completedSection == null ||
            unfinishedList == null ||
            completedList == null ||
            itemPrefab == null)
        {
            Debug.LogError(
                "QuestPageUI：Inspector 中还有引用没有绑定。",
                this);
            return;
        }

        manager = QuestManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning(
                "QuestPageUI：未找到 QuestManager，请从 MenuScene 开始运行。",
                this);
            return;
        }

        // 页面打开时，开始监听任务变化。
        manager.OnQuestsChanged += Refresh;

        // 先显示当前已有的任务。
        Refresh();
    }

    private void OnDisable()
    {
        // 页面关闭时，停止监听。
        if (manager != null)
        {
            manager.OnQuestsChanged -= Refresh;
        }

        manager = null;
    }

    private void Refresh()
    {
        if (manager == null)
            return;

        ClearList(unfinishedList);
        ClearList(completedList);

        int unfinishedCount = 0;
        int completedCount = 0;

        foreach (QuestData data in manager.Quests)
        {
            QuestState state = manager.GetState(data.questId);

            if (state == null)
                continue;

            Transform parent;

            if (state.status == QuestStatus.InProgress)
            {
                parent = unfinishedList;
                unfinishedCount++;
            }
            else if (state.status == QuestStatus.Completed)
            {
                parent = completedList;
                completedCount++;
            }
            else
            {
                // 尚未解锁的任务不显示。
                continue;
            }

            QuestItemUI item = Instantiate(itemPrefab, parent, false);
            item.Setup(data, state);
        }

        unfinishedSection.SetCount(unfinishedCount);
        completedSection.SetCount(completedCount);
    }

    private void ClearList(Transform listRoot)
    {
        for (int i = listRoot.childCount - 1; i >= 0; i--)
        {
            GameObject item = listRoot.GetChild(i).gameObject;

            // 先退出显示和布局，再安排销毁。
            item.SetActive(false);
            Destroy(item);
        }
    }
}