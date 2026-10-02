using UnityEngine;
using TMPro;

public class QuestItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text progressText;

    public void Setup(QuestData data, QuestState state)
    {
        if (data == null || state == null)
            return;

        bool completed = state.status == QuestStatus.Completed;

        titleText.text = completed
            ? $"<s>{data.questName}</s>"
            : data.questName;

        progressText.text = completed
    ? $"已完成：{state.progress}/{data.requiredCount}"
    : $"进度：{state.progress}/{data.requiredCount}";
    }
}