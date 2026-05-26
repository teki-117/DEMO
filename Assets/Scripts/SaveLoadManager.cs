using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Unity 脚本 3 个引用
public class SaveLoadManager : MonoBehaviour
{
    public GameObject saveLoadPanel;
    public TextMeshProUGUI panelTitle;
    public Button[] saveLoadButtons;
    public Button prevPageButton;
    public Button nextPageButton;
    public Button backButton;

    private bool isSave;
    private int currentPage = Constants.DEFAULT_START_INDEX;
    private readonly int slotsPerPage = Constants.SLOTS_PER_PAGE;
    private readonly int totalSlots = Constants.TOTAL_SLOTS;

    public static SaveLoadManager Instance { get; private set; }
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
        prevPageButton.onClick.AddListener(PrevPage);
        nextPageButton.onClick.AddListener(NextPage);
        backButton.onClick.AddListener(GoBack);
        saveLoadPanel.SetActive(false);
    }
    public void ShowSaveLoadUI(bool save)
    {
        isSave = save;
        panelTitle.text = isSave ? Constants.SAVE_GAME : Constants.LOAD_GAME;
        UpdateSaveLoadUI();
        saveLoadPanel.SetActive(true);
        LoadStorylineAndScreenshots();
    }
    //string GetLocalized(string key)
    //{
    //    return LocalizationManager.Instance.GetLocalizedValue(key);
    //}
    private void UpdateSaveLoadUI()
    {
        for (int i = 0; i < slotsPerPage; i++)
        {
            int slotIndex = currentPage * slotsPerPage + i;
            if (slotIndex < totalSlots)
            {
                saveLoadButtons[i].gameObject.SetActive(true);
                saveLoadButtons[i].interactable = true;

                // 更新按钮文本和图片
                var slotText = (slotIndex + 1) + Constants.COLON + Constants.EMPTY_SLOT;
                var textComponents = saveLoadButtons[i].GetComponentsInChildren<TextMeshProUGUI>();
                textComponents[0].text = null;
                textComponents[1].text = slotText;
                saveLoadButtons[i].GetComponentInChildren<RawImage>().texture = null;
            }
            else
            {
                saveLoadButtons[i].gameObject.SetActive(false);
            }
        }
    }
    private void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateSaveLoadUI();
            LoadStorylineAndScreenshots();
        }
    }

    private void NextPage()
    {
        if ((currentPage + 1) * slotsPerPage < totalSlots)
        {
            currentPage++;
            UpdateSaveLoadUI();
            LoadStorylineAndScreenshots();
        }
    }

    private void GoBack()
    {
        saveLoadPanel.SetActive(false);
    }

    private void LoadStorylineAndScreenshots()
    {
    }
}






























