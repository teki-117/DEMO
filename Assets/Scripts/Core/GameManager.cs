using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public string playerName;
    public string currentScene;
    public string currentStoryFile;
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
    public LinkedList<ExcelData> historyRecords; // 保存历史记录
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
}
