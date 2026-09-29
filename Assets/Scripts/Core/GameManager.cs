using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public string playerName;
    public string currentScene;
    public string currentStoryFile;
    public string storyReturnScene = Constants.MENU_SCENE;
    public int currentLineIndex;
    public int currentLanguageIndex = Constants.DEFAULT_LANGUAGE_INDEX;
    public string currentLanguage = Constants.DEFAULT_LANGUAGE;
    public string currentBackgroundImg;
    public string currentBackgroundMusic;
    public List<CharacterSaveData> currentCharacterData = new List<CharacterSaveData>(); // 当前场景的角色数据

    public string WinStoryFileName;
    public string LoseStoryFileName;

    public bool hasStarted;
    public HashSet<string> unlockedBackgrounds = new HashSet<string>(); // 保存已解锁的背景
    public Dictionary<string, int> maxReachedLineIndices = new Dictionary<string, int>(); // 全局存储每个文件的最远行索引
    public LinkedList<ExcelData> historyRecords = new LinkedList<ExcelData>(); // 保存历史记录
    public enum SaveLoadMode { None, Save, Load }
    public SaveLoadMode currentSaveLoadMode { get; set; } = SaveLoadMode.None;
    public SaveData pendingData;
    public void Save(int slotIndex)
    {
        string path = GenerateDataPath(slotIndex);
        File.WriteAllText(path, JsonConvert.SerializeObject(pendingData, Formatting.Indented));
    }
    public void Load(int slotIndex)
    {
        string path = GenerateDataPath(slotIndex);
        pendingData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(path));
    }
    public string GenerateDataPath(int index)
    {
        return Path.Combine(Application.persistentDataPath, Constants.SAVE_FILE_PATH, index + Constants.SAVE_FILE_EXTENSION);
    }
    public static GameManager Instance { get; private set; }
    // 开始一轮全新的游戏。
    // 只有玩家点击“新游戏”时才调用。
    public void StartNewGame()
    {
        playerName = string.Empty;
        hasStarted = false;
        currentSaveLoadMode = SaveLoadMode.None;

        historyRecords = new LinkedList<ExcelData>();
        storyReturnScene = Constants.MENU_SCENE;

        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.ResetTime();
        }

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.ResetWorld();
        }
        // 新游戏重置好感度。
        CharacterStateManager.Instance.ResetAffection();

        PrepareStory(Constants.DEFAULT_STORY_FILE);
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景保持
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // 准备进入一段新的剧情。
    // 从地图进入学校等事件时调用，不重置好感度。
    public void PrepareStory(string storyFileName)
    {
        // 清除尚未使用的存档数据，避免进入剧情时被旧数据覆盖。
        pendingData = null;

        currentStoryFile = storyFileName;
        currentLineIndex = Constants.DEFAULT_START_LINE;

        // 清除上一段剧情的画面状态。
        currentBackgroundImg = string.Empty;
        currentBackgroundMusic = string.Empty;
        currentCharacterData.Clear();

        // 防止历史记录尚未初始化。
        if (historyRecords == null)
        {
            historyRecords = new LinkedList<ExcelData>();
        }
    }
}
