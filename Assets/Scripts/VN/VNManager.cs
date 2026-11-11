using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VNManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI speakerName;
    [SerializeField] private TypewriterEffect typewriterEffect;
    [SerializeField] private ScreenShotter screenShotter;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image backgroundImage;

    [Header("Bottom Buttons")]
    [SerializeField] private GameObject bottomButtons;
    [SerializeField] private Button autoButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    //[SerializeField] private Button quickSaveButton;
    //[SerializeField] private Button quickLoadButton;
    [SerializeField] private Button historyButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button closeButton;

    private List<ExcelData> storyData;
    private string currentStoryFileName;
    private int currentLine;
    private string currentSpeakingContent;
    private float currentTypingSpeed = Constants.DEFAULT_TYPING_SPEED;
    private int maxReachedLineIndex = 0;

    private readonly Dictionary<string, CharacterSaveData> charStates = new();

    public static VNManager Instance { get; private set; }

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

    // ===== 启动 =====

    private void Start()
    {
        var gm = GameManager.Instance;
        gm.hasStarted = true;
        gm.currentScene = Constants.GAME_SCENE;

        if (gm.pendingData != null)
        {
            var savedData = gm.pendingData;
            gm.pendingData = null;

            gm.currentStoryFile = savedData.savedStoryFileName;
            savedData.savedLine--;
            gm.currentLineIndex = savedData.savedLine;

            savedData.savedHistoryRecords.RemoveLast();
            gm.historyRecords = savedData.savedHistoryRecords;
            gm.playerName = savedData.savedPlayerName;

            gm.currentBackgroundImg = savedData.savedBackgroundImg;
            gm.currentBackgroundMusic = savedData.savedBackgroundMusic;

            gm.currentCharacterData = savedData.savedCharacters;

            CharacterStateManager.Instance.LoadStates(savedData.savedAffection);
        }
        currentLine = gm.currentLineIndex;

        UI_AddListeners();
        Render_InitializeImages();
        Flow_LoadStory(gm.currentStoryFile);
        Flow_Next();
    }

    // ===== 输入与每帧更新 =====

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (!dialogueBox.activeSelf)
            {
                UI_Open();
            }
            else if (!PointerOverAnyUI() && !ChoiceManager.Instance.choicePanel.activeSelf)
            {
                Flow_Next();
            }
        }
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (dialogueBox.activeSelf)
            {
                UI_Close();
            }
            else
            {
                UI_Open();
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            SetMode(AdvanceMode.HoldCtrl);
        }

    }
    private bool PointerOverAnyUI()
    {
        if (EventSystem.current == null) return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    // ===== 剧情流程 =====

    private enum LineKind
    {
        Dialogue,
        Choice,
        Goto,
        Game,
        End,
        QuestUnlock,
        LocationUnlock
    }

    private enum AdvanceMode { Manual, Auto, Skip, HoldCtrl }

    private AdvanceMode mode = AdvanceMode.Manual;
    private Coroutine advanceCo;

    private LineKind GetKind(ExcelData d)
    {
        var s = d.speakerName;

        if (s == Constants.CHOICE) return LineKind.Choice;
        if (s == Constants.GOTO) return LineKind.Goto;
        if (s == Constants.GAME) return LineKind.Game;
        if (s == Constants.END_OF_STORY) return LineKind.End;
        if (s == Constants.QUEST_UNLOCK) return LineKind.QuestUnlock;
        if (s == Constants.LOCATION_UNLOCK) return LineKind.LocationUnlock;

        return LineKind.Dialogue;
    }
    private void OnDisable()
    {
        mode = AdvanceMode.Manual;
        if (advanceCo != null) StopCoroutine(advanceCo);
        DOTween.Kill(this);
    }
    private void Flow_Next()
    {
        if (typewriterEffect != null && typewriterEffect.IsTyping())
        {
            typewriterEffect.CompleteLine();
            return;
        }

        if (storyData == null || storyData.Count == 0)
        {
            SetMode(AdvanceMode.Manual);
            Debug.LogError("剧情数据为空，已停止推进。", this);
            return;
        }

        if (currentLine < 0 || currentLine >= storyData.Count)
        {
            SetMode(AdvanceMode.Manual);
            Debug.LogError(
                $"剧情行号越界：{currentLine}，请检查剧情结束指令。",
                this);
            return;
        }

        if (currentLine > maxReachedLineIndex)
        {
            maxReachedLineIndex = currentLine;
            var dict = GameManager.Instance.maxReachedLineIndices;
            dict[currentStoryFileName] = maxReachedLineIndex;
        }

        var d = storyData[currentLine];
        switch (GetKind(d))
        {
            case LineKind.Choice:
                Choices_Show();
                return;

            case LineKind.Goto:
                Flow_LoadStory(d.speakingContent);
                currentLine = Constants.DEFAULT_START_LINE;
                Flow_Next();
                return;

            case LineKind.Game:
                Live2DGameScene(d);
                return;

            case LineKind.QuestUnlock:
                Flow_UnlockQuest(d.speakingContent);

                currentLine++;
                Flow_Next();
                return;

            case LineKind.LocationUnlock:
                if (WorldManager.Instance != null)
                    WorldManager.Instance.UnlockLocation(d.speakingContent);
                else
                    Debug.LogError("剧情无法解锁地点：找不到 WorldManager。", this);

                currentLine++;
                Flow_Next();
                return;

            case LineKind.End:
                FinishStory();
                return;

            default:
                Flow_DisplayThisLine();
                return;


        }
    }
    private void Flow_DisplayThisLine()
    {
        var gm = GameManager.Instance;
        gm.currentLineIndex = currentLine;

        var data = storyData[currentLine];

        speakerName.text = LM.GetSpeakerName(data);
        currentSpeakingContent = LM.GetSpeakingContent(data);
        typewriterEffect.StartTyping(currentSpeakingContent, currentTypingSpeed);

        gm.historyRecords.AddLast(data);

        if (!string.IsNullOrEmpty(data.avatarImageFileName))
            Render_UpdateAvatar(data.avatarImageFileName);
        else
            avatarImage.gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(data.vocalAudioFileName))
            Audio_PlayVoice(data.vocalAudioFileName);

        if (!string.IsNullOrEmpty(data.backgroundImageFileName))
        {
            gm.currentBackgroundImg = data.backgroundImageFileName;
            Render_UpdateBackground(data.backgroundImageFileName);
        }

        ApplyCharacterCommands(data.characterCommands);

        currentLine++;
    }
    private void ApplyCharacterCommands(List<CharacterCommand> cmds)
    {
        if (cmds == null) return;

        var gm = GameManager.Instance;

        foreach (var cmd in cmds)
        {
            if (cmd.action == Constants.DISAPPEAR)
            {
                charStates.Remove(cmd.characterID);
                gm.currentCharacterData.RemoveAll(c => c.characterID == cmd.characterID);
                CharacterManager.Instance.HideCharacter(cmd.characterID);
            }
            else
            {
                var state = new CharacterSaveData
                {
                    characterID = cmd.characterID,              
                    positionX = cmd.positionX,
                    scale = cmd.scale,
                    expressionName = cmd.expressionName,
                    motionKey = cmd.motionKey
                };

                charStates[cmd.characterID] = state;
                gm.currentCharacterData.RemoveAll(c => c.characterID == cmd.characterID);
                gm.currentCharacterData.Add(state);
                if (cmd.action.StartsWith(Constants.APPEAR_AT))
                {
                    CharacterManager.Instance.ShowCharacter(
                        cmd.characterID,
                        new Vector2(cmd.positionX, 0f),
                        new Vector2(cmd.scale,cmd.scale),
                        cmd.expressionName,
                        cmd.motionKey
                    );
                }
                else if (cmd.action.StartsWith(Constants.MOVE_TO))
                {
                    CharacterManager.Instance.MoveCharacter(
                        cmd.characterID,
                        cmd.positionX,
                        Constants.DURATION_TIME
                    );
                }
            }
        }
    }
    private void ReturnToMenu()
    {
        GameManager.Instance.hasStarted = false;
        SceneManager.LoadScene(Constants.MENU_SCENE);
    }

    private void FinishStory()
    {
        // 结束自动播放或快进。
        SetMode(AdvanceMode.Manual);

        var gm = GameManager.Instance;

        string targetScene = string.IsNullOrEmpty(gm.storyReturnScene)
            ? Constants.MENU_SCENE
            : gm.storyReturnScene;

        gm.currentScene = targetScene;

        // 返回地图时，游戏仍在进行。
        gm.hasStarted = targetScene != Constants.MENU_SCENE;

        if (targetScene == "MapScene" && WorldManager.Instance != null)
        {
            WorldManager.Instance.CompleteCurrentAction();
        }

        SceneManager.LoadScene(targetScene);
    }
    private void SetMode(AdvanceMode m)
    {
        if (mode == m) return;

        mode = m;

        if (advanceCo != null)
            StopCoroutine(advanceCo);

        if (mode != AdvanceMode.Manual)
            advanceCo = StartCoroutine(AutoAdvanceLoop());
        else
            currentTypingSpeed = Constants.DEFAULT_TYPING_SPEED;
    }

    private bool CanSkip() => currentLine < maxReachedLineIndex;

    private IEnumerator AutoAdvanceLoop()
    {
        while (mode != AdvanceMode.Manual)
        {
            if (mode == AdvanceMode.HoldCtrl &&
                !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                SetMode(AdvanceMode.Manual);
                yield break;
            }

            if (mode == AdvanceMode.Skip && !CanSkip())
            {
                SetMode(AdvanceMode.Manual);
                yield break;
            }

            if (!typewriterEffect.IsTyping())
            {
                if (mode == AdvanceMode.Skip || mode == AdvanceMode.HoldCtrl)
                    currentTypingSpeed = Constants.SKIP_MODE_TYPING_SPEED;

                Flow_Next();
            }

            float wait = (mode == AdvanceMode.Auto)
                ? Constants.DEFAULT_AUTO_WAITING_SECONDS
                : Constants.DEFAULT_SKIP_WAITING_SECONDS;

            yield return new WaitForSeconds(wait);
        }
    }

    // ===== 选项 =====

    private void Choices_Show()
    {
        var choices = Choices_ParseCurrent();
        ChoiceManager.Instance.ShowChoices(choices, Choices_HandleSelect);
    }

    private void Choices_HandleSelect(string selected)
    {
        currentLine = Constants.DEFAULT_START_LINE;

        //if (Constants.ALL_MEMORIES.Contains(selected))
        //    GameManager.Instance.unlockedMemories.Add(selected);

        Flow_LoadStory(selected);
        Flow_Next();
    }
    private List<ChoiceOption> Choices_ParseCurrent()
    {
        var data = storyData[currentLine];

        string[] choiceTexts = LM.GetSpeakingContent(data)
            .Split(Constants.ChoiceDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray();

        string[] targets = (data.avatarImageFileName ?? "")
            .Split(Constants.ChoiceDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray();

        string[] changes = (data.vocalAudioFileName ?? "")
            .Split(Constants.ChoiceDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray();

        string[] conditions = (data.backgroundImageFileName ?? "")
            .Split(Constants.ChoiceDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray();

        var list = new List<ChoiceOption>();
        int n = choiceTexts.Length;
        for (int i = 0; i < n; i++)
        {
            list.Add(new ChoiceOption
            {
                text = choiceTexts.ElementAtOrDefault(i) ?? "",
                nextStoryFileName = targets.ElementAtOrDefault(i) ?? "",
                changes = ParseChanges(changes.ElementAtOrDefault(i)),
                conditions = ParseConditions(conditions.ElementAtOrDefault(i))
            });
        }
        return list;
    }
    // "Alice:+10;Bob:-5"
    private List<AffectionChange> ParseChanges(string raw)
    {
        var list = new List<AffectionChange>();
        if (string.IsNullOrEmpty(raw)) return list;

        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length != 2) continue;
            if (int.TryParse(kv[1], out int delta))
                list.Add(new AffectionChange { characterID = kv[0].Trim(), delta = delta });
        }

        return list;
    }

    // "Annie:0,100;Bob:-50,50"
    private List<AffectionCondition> ParseConditions(string raw)
    {
        var list = new List<AffectionCondition>();
        if (string.IsNullOrEmpty(raw)) return list;

        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length != 2) continue;

            var id = kv[0].Trim();
            var nums = kv[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (nums.Length != 2) continue;

            if (int.TryParse(nums[0], out int min) && int.TryParse(nums[1], out int max))
                list.Add(new AffectionCondition { characterID = id, minValue = min, maxValue = max });
        }

        return list;
    }

    // ===== 画面显示 =====

    private void Render_InitializeImages()
    {
        backgroundImage.gameObject.SetActive(false);
        avatarImage.gameObject.SetActive(false);
    }

    internal void Flow_LoadStory(string fileName)
    {
        currentStoryFileName = fileName;

        var path = System.IO.Path.Combine(
            Application.streamingAssetsPath,
            Constants.STORY_PATH,
            fileName + Constants.STORY_FILE_EXTENSION
        );

        storyData = ExcelReader.ReadExcel(path);

        if (storyData == null || storyData.Count == 0)
        {
            Debug.LogError(Constants.NO_DATA_FOUND);
        }
        GameManager.Instance.currentStoryFile = currentStoryFileName;

        // 最远行索引
        var dict = GameManager.Instance.maxReachedLineIndices;
        if (dict.TryGetValue(currentStoryFileName, out var idx))
            maxReachedLineIndex = idx;
        else
        {
            maxReachedLineIndex = 0;
            dict[currentStoryFileName] = 0;
        }

        // 恢复背景、BGM、角色
        Render_RecoverBG_BGM_Characters();
    }
    private void Render_RecoverBG_BGM_Characters()
    {
        var gm = GameManager.Instance;

        if (!string.IsNullOrEmpty(gm.currentBackgroundImg))
            Render_UpdateBackground(gm.currentBackgroundImg);

        if (!string.IsNullOrEmpty(gm.currentBackgroundMusic))
            Audio_PlayBGM(gm.currentBackgroundMusic);

        CharacterManager.Instance.ClearAll();
        foreach (var c in gm.currentCharacterData)
        {
            charStates[c.characterID] = c;
            CharacterManager.Instance.ShowCharacter(
                c.characterID,
                new Vector2(c.positionX, 0f),
                new Vector2(c.scale, c.scale),
                c.expressionName
            );
        }
    }
    private void Render_UpdateAvatar(string fileName)
    {
        var path = Constants.AVATAR_PATH + fileName;
        Render_UpdateImage(path, avatarImage, doFadeIn: false);
    }

    private void Render_UpdateBackground(string fileName)
    {
        var path = Constants.BACKGROUND_PATH + fileName;
        Render_UpdateImage(path, backgroundImage, doFadeIn: false);

        var set = GameManager.Instance.unlockedBackgrounds;
        if (!set.Contains(fileName)) set.Add(fileName);
    }

    private void Render_UpdateImage(string resourcePath, Image target, bool doFadeIn)
    {
        var sprite = SpriteCache.Get(resourcePath);
        if (sprite == null) return;

        target.sprite = sprite;
        target.gameObject.SetActive(true);
        
        if (doFadeIn)
        {
            var cg = target.canvasRenderer;
            target.DOKill();
            target.DOFade(1f, Constants.DURATION_TIME).From(0f).SetId(this);
        }
    }

    // ===== 界面按钮 =====

    private void UI_AddListeners()
    {
        autoButton.onClick.AddListener(() =>
        {
            // 切换 Auto <-> Manual
            SetMode(mode == AdvanceMode.Auto ? AdvanceMode.Manual : AdvanceMode.Auto);
        });

        skipButton.onClick.AddListener(() =>
        {
            if (mode != AdvanceMode.Skip && CanSkip()) SetMode(AdvanceMode.Skip);
            else SetMode(AdvanceMode.Manual);
        });

        saveButton.onClick.AddListener(UI_OnSave);
        loadButton.onClick.AddListener(UI_OnLoad);
        //quickSaveButton.onClick.AddListener(UI_OnQuickSave);
        //quickLoadButton.onClick.AddListener(UI_OnQuickLoad);
        historyButton.onClick.AddListener(() => SceneManager.LoadScene(Constants.HISTORY_SCENE));
        settingButton.onClick.AddListener(() => SceneManager.LoadScene(Constants.SETTING_SCENE));
        homeButton.onClick.AddListener(() => SceneManager.LoadScene(Constants.MENU_SCENE));
        closeButton.onClick.AddListener(UI_Close);

        //if (GameManager.Instance.isMemoryMode)
        //{
        //    saveButton.gameObject.SetActive(false);
        //    loadButton.gameObject.SetActive(false);
        //    quickSaveButton.gameObject.SetActive(false);
        //    quickLoadButton.gameObject.SetActive(false);
        //}
    }
    private void UI_Open()
    {
        dialogueBox.SetActive(true);
        bottomButtons.SetActive(true);
    }
    private void UI_Close()
    {
        dialogueBox.SetActive(false);
        bottomButtons.SetActive(false);
    }

    // ===== 音频 =====

    private void Audio_PlayVoice(string file) => AudioManager.Instance.PlayVoice(file);

    private void Audio_PlayBGM(string file) => AudioManager.Instance.PlayBackground(file);

    // ===== 任务 =====

    private void Flow_UnlockQuest(string rawQuestId)
    {
        string questId = rawQuestId?.Trim();

        if (string.IsNullOrEmpty(questId))
        {
            Debug.LogError("剧情任务指令缺少任务 ID。", this);
            return;
        }

        var manager = QuestManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "剧情无法解锁任务：找不到 QuestManager。",
                this);
            return;
        }

        QuestData data = manager.GetQuestData(questId);

        if (data == null)
        {
            Debug.LogError(
                $"剧情中的任务 ID 没有注册：{questId}",
                this);
            return;
        }

        if (manager.UnlockQuest(questId))
        {
            Debug.Log(
                $"剧情解锁任务：{data.questName}（{questId}）",
                this);
        }
    }

    // ===== 小游戏场景 =====

    private void Live2DGameScene(ExcelData data)
    {
        string targetSceneName = data.speakingContent;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            targetSceneName = Constants.LIVE2D_GAME_SCENE;
        }

        GameManager.Instance.currentStoryFile = currentStoryFileName;

        // 回来以后不会重复执行 game 这一行，要从下一行继续
        GameManager.Instance.currentLineIndex = currentLine + 1;

        GameManager.Instance.currentScene = Constants.GAME_SCENE;

        SceneManager.LoadScene(targetSceneName);
    }

    // ===== 存档读档 =====

    private void UI_OnSave()
    {
        Save_ToPending();
        GameManager.Instance.currentSaveLoadMode = GameManager.SaveLoadMode.Save;
        SceneManager.LoadScene(Constants.SAVE_LOAD_SCENE);
    }

    private void UI_OnLoad()
    {
        GameManager.Instance.currentSaveLoadMode = GameManager.SaveLoadMode.Load;
        SceneManager.LoadScene(Constants.SAVE_LOAD_SCENE);
    }

    //private void UI_OnQuickSave()
    //{
    //    Save_ToPending();
    //    GameManager.Instance.Save(Constants.QUICK_SAVE_SLOT);
    //}

    //private void UI_OnQuickLoad()
    //{
    //    var gm = GameManager.Instance;
    //    string dataPath = gm.GenerateDataPath(Constants.QUICK_SAVE_SLOT);

    //    if (File.Exists(dataPath))
    //    {
    //        gm.Load(Constants.QUICK_SAVE_SLOT);
    //        SceneManager.LoadScene(Constants.GAME_SCENE);
    //    }
    //}
    private void Save_ToPending()
    {
        UI_Close();
        Texture2D shot = screenShotter.CaptureScreenshot();
        UI_Open();

        var gm = GameManager.Instance;

        var historyCopy = new LinkedList<ExcelData>(gm.historyRecords);
        var charCopy = new List<CharacterSaveData>(gm.currentCharacterData);

        var png = shot.EncodeToPNG();
        Destroy(shot);

        gm.pendingData = new SaveData
        {
            savedStoryFileName = currentStoryFileName,
            savedLine = currentLine,
            savedScreenshotData = png,
            savedHistoryRecords = historyCopy,
            savedPlayerName = gm.playerName,
            savedBackgroundImg = gm.currentBackgroundImg,
            savedBackgroundMusic = gm.currentBackgroundMusic,
            savedCharacters = charCopy,
            savedAffection = CharacterStateManager.Instance.DumpStates(),
            savedScene = Constants.GAME_SCENE,
            savedStoryReturnScene = gm.storyReturnScene,
            rpg = WorldManager.Instance != null
                ? WorldManager.Instance.CaptureRpgSave()
                : null
        };
    }
}
