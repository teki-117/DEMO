using UnityEngine;
using TMPro;
public class QuestSectionUI : MonoBehaviour
{
    [SerializeField] private GameObject listRoot;
    [SerializeField] private TMP_Text headerLabel;
    [SerializeField] private string sectionName;

    private int questCount;

    public void SetCount(int count)
    {
        questCount = Mathf.Max(0, count);
        RefreshHeader();
    }
    public void Toggle()
    {
        if (listRoot == null)
            return;

        listRoot.SetActive(!listRoot.activeSelf);
        RefreshHeader();
    }
    private void Awake()
    {
        RefreshHeader();
    }

    private void RefreshHeader()
    {
        if (listRoot == null || headerLabel == null)
            return;

        string arrow = listRoot.activeSelf ? "▼" : "▶";
        headerLabel.text = $"{arrow} {sectionName}（{questCount}）";
    }
}