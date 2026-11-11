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
            LoadSlot(slotIndex);
        }
        else
        {
            ShowConfirm(
                LM.GLV(Constants.CONFIRM_COVER_SAVE_FILE),
                () => { SaveToSlot(slotIndex, slot); }
            );
        }
    }
    // 读档：先恢复 RPG 数据，再按存档时所在的场景进入剧情或地图。
    private void LoadSlot(int slotIndex)
    {
        var gm = GameManager.Instance;
        gm.Load(slotIndex);

        SaveData data = gm.pendingData;

        if (WorldManager.Instance != null)
            WorldManager.Instance.RestoreRpgSave(data.rpg);

        // 旧存档没有这个字段时保持原值。
        if (!string.IsNullOrEmpty(data.savedStoryReturnScene))
            gm.storyReturnScene = data.savedStoryReturnScene;

        if (data.savedScene == "MapScene")
        {
            // 地图存档：剧情画面数据用不到，这里直接恢复名字、好感度和历史记录。
            gm.pendingData = null;
            gm.playerName = data.savedPlayerName;

            if (data.savedHistoryRecords != null)
                gm.historyRecords = data.savedHistoryRecords;

            if (data.savedAffection != null && CharacterStateManager.Instance != null)
                CharacterStateManager.Instance.LoadStates(data.savedAffection);

            gm.hasStarted = true;
            gm.currentScene = "MapScene";
            SceneManager.LoadScene("MapScene");
            return;
        }

        SceneManager.LoadScene(Constants.GAME_SCENE);
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






























