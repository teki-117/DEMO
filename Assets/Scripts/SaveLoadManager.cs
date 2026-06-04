using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Unity 脚本 3 个引用
public class SaveLoadManager : MonoBehaviour
{ 
    public TextMeshProUGUI panelTitle;
    public SaveSlot[] slots;
    public Button prevPageButton;
    public Button nextPageButton;
    public Button backButton;

    public GameObject confirmPanel;
    public TextMeshProUGUI confirmText;
    public Button confirmButton;
    public Button cancelButton;

    private int currentPage = Constants.DEFAULT_START_INDEX;
    private readonly int slotsPerPage = Constants.SLOTS_PER_PAGE;
    private readonly int totalSlots = Constants.TOTAL_SLOTS;
    private bool isLoad => GameManager.Instance.currentSaveLoadMode == GameManager.SaveLoadMode.Load;
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
        panelTitle.text = isLoad ? LM.GLV(Constants.LOAD_GAME) : LM.GLV(Constants.SAVE_GAME);

        prevPageButton.GetComponentInChildren<TextMeshProUGUI>().text = LM.GLV(Constants.PREV_PAGE);
        nextPageButton.GetComponentInChildren<TextMeshProUGUI>().text = LM.GLV(Constants.NEXT_PAGE);
        backButton.GetComponentInChildren<TextMeshProUGUI>().text = LM.GLV(Constants.BACK);

        prevPageButton.onClick.AddListener(PrevPage);
        nextPageButton.onClick.AddListener(NextPage);
        backButton.onClick.AddListener(GoBack);

        confirmPanel.SetActive(false);

        RefreshPage();
    }
    public void HandleEmptySlot(int slotIndex, SaveSlot slot)
    {
        SaveToSlot(slotIndex, slot);
    }
    public void HandleExistingSlot(int slotIndex, SaveSlot slot)
    {
        if (isLoad)
        {
            GameManager.Instance.Load(slotIndex);
            SceneManager.LoadScene(Constants.GAME_SCENE);
        }
        else
        {
            ShowConfirm(
                LM.GLV(Constants.CONFIRM_COVER_SAVE_FILE),
                () => { SaveToSlot(slotIndex, slot); }
            );
        }
    }
    public void RequestDelete(int slotIndex, SaveSlot slot)
    {
        ShowConfirm(
              LM.GLV(Constants.CONFIRM_DELETE_SAVE_FILE),
              () => { DeleteSlot(slotIndex, slot); }
          );
    }
    private void SaveToSlot(int slotIndex, SaveSlot slot)
    {
        GameManager.Instance.Save(slotIndex);
        slot.Refresh();
    }
    private void DeleteSlot(int slotIndex, SaveSlot slot)
    {
        File.Delete(GameManager.Instance.GenerateDataPath(slotIndex));
        slot.Refresh();
    }
    private void ShowConfirm(string msg, System.Action onYes)
    {
        confirmText.text = msg;
        confirmPanel.SetActive(true);

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() =>
        {
            confirmPanel.SetActive(false);
            onYes?.Invoke();
        });
        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() => confirmPanel.SetActive(false));
    }
    private void RefreshPage()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int slotIndex = currentPage * slotsPerPage + i;
            if (slotIndex >= totalSlots)
            {
                slots[i].gameObject.SetActive(false);
                continue;
            }
            slots[i].gameObject.SetActive(true);
            slots[i].Init(this, slotIndex);
            slots[i].Refresh();
        }
    }
    private void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshPage();
        }
    }
    private void NextPage()
    {
        if ((currentPage + 1) * slotsPerPage < totalSlots)
        {
            currentPage++;
            RefreshPage();
        }
    }
    private void GoBack()
    {
        var sceneName = GameManager.Instance.currentScene;
        if (sceneName == Constants.GAME_SCENE)
        {
            GameManager.Instance.historyRecords.RemoveLast();
        }
        GameManager.Instance.pendingData = null;
        SceneManager.LoadScene(sceneName);
    }
    
  
}






























