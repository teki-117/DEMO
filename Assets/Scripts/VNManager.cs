using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core.Easing;


public class VNManager : MonoBehaviour
{
    public GameObject gamePanel;
    public GameObject dialogueBox;
    public TextMeshProUGUI speakerName;
    public TextMeshProUGUI speakingContent;
    public TypewriteEffect typewriteEffect;
    public Image avatarImage;
    public AudioSource vocalAudio;
    public Image backgroundImage;
    public AudioSource backgroundMusic;
    public Image characterImage1;
    public Image characterImage2;

    public GameObject choicePanel;
    public Button choiceButton1;
    public Button choiceButton2;

    public GameObject bottomButtons;
    public Button autoButton;
    public Button skipButton;
    public Button saveButton;
    public Button loadButton;
    public Button historyButton;
    public Button settingsButton;
    public Button homeButton;
    public Button closeButton;

    private string storyPath = Constants.STORY_PATH;
    private string defaultStoryFileName = Constants.DEFAULT_STORY_FILE_NAME;
    private string excelFileExtension = Constants.EXCEL_FILE_EXTENSION;
    private List<ExcelReader.ExcelData> storyData;
    private int currentLine;
    private string currentStoryFilName;

    private bool isAutoPlay = false;
    private bool isSkip = false;
    private int maxReachedLineIndex = 0;
    private Dictionary<string, int> globalMaxReachedLineIndices = new Dictionary<string, int>();
    public static VNManager Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null)
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
        bottomButtonsAddListener();
        gamePanel.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        if (gamePanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (!dialogueBox.activeSelf)
            {
                OpenUI();
            }
            else if (!IsHittingBottomButtons())
            {
                if (!SaveLoadManager.Instance.saveLoadPanel.activeSelf)
                {
                    DisplayNextLine();
                }
            }
        }
    }

    void bottomButtonsAddListener()
    {
        autoButton.onClick.AddListener(OnAutoButtonClick);
        skipButton.onClick.AddListener(OnSkipButtonClick);
        saveButton.onClick.AddListener(OnSaveButtonClick);
        loadButton.onClick.AddListener(OnLoadButtonClick);

        homeButton.onClick.AddListener(OnHomeButtonClick);
        closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    public void StartGame()
    {
        InitializeAndLoadStory(defaultStoryFileName);
    }
    void InitializeAndLoadStory(string fileName)
    {
        Initialize();
        LoadStoryFromFile(fileName);
        DisplayNextLine();
    }
    void Initialize()
    {
        currentLine = Constants.DEFAULT_START_LINE;

        avatarImage.gameObject.SetActive(false);
        backgroundImage.gameObject.SetActive(false);
        characterImage1.gameObject.SetActive(false);
        characterImage2.gameObject.SetActive(false);
        choicePanel.SetActive(false);
        
    }
    void LoadStoryFromFile(string fileName)
    {
        currentStoryFilName = fileName;
        var path = storyPath + fileName + excelFileExtension;
        storyData = ExcelReader.ReadExcel(path);
        if (storyData == null || storyData.Count == 0)
        {
            Debug.LogError(Constants.NO_LOAD_FOUND);
        }
        if (globalMaxReachedLineIndices.ContainsKey(currentStoryFilName))
        {
            maxReachedLineIndex = globalMaxReachedLineIndices[currentStoryFilName];
        }
        else
        {
            maxReachedLineIndex = 0;
            globalMaxReachedLineIndices[currentStoryFilName] = maxReachedLineIndex;
        }
    }

    void DisplayNextLine()
    {
        if(currentLine > maxReachedLineIndex)
        {
            maxReachedLineIndex = currentLine;
            globalMaxReachedLineIndices[currentStoryFilName ] = maxReachedLineIndex;
        }
        if (currentLine >= storyData.Count - 1)
        {
            if(isAutoPlay)
            {
                isAutoPlay= false;
                UpdateButtonImage(Constants.AUTO_OFF, autoButton);
            }
            if (storyData[currentLine].speakerName == Constants.END_OF_STORY)
            {
                Debug.Log(Constants.END_OF_STORY);

            }
            if (storyData[currentLine].speakerName == Constants.CHOICE)
            {
                ShowChoices();
               
            }
            return;
        }
        if (typewriteEffect.IsTyping())
        {
            typewriteEffect.CompleteLine();
        }
        else
        {
            DisplayThisLine();
        }
    }

    void DisplayThisLine()
    {
        var data = storyData[currentLine];
        speakerName.text = data.speakerName;
        speakingContent.text = data.speakingContent;
        typewriteEffect.StartTyping(speakingContent.text);
        if (NotNullNorEmpty(data.avatarImageFileName)) 
        {
            UpdateAvatarImage(data.avatarImageFileName);
        }
       else
        { 
            avatarImage.gameObject.SetActive(false);
        }
        if (NotNullNorEmpty(data.vocalAudioFileName))
        {
            PlayVocalAudio(data.vocalAudioFileName);
        }
        if(NotNullNorEmpty(data.backgroundImageFileName))
        {
            UpdateBackgroundImage(data.backgroundImageFileName);
        }
        if (NotNullNorEmpty(data.backgroundMusicFileName))
        {
            PlayBackgroundMusic(data.backgroundMusicFileName);
        }
        if (NotNullNorEmpty(data.character1Action))
        {
            UpdateCharacterImage(data.character1Action,data.character1ImageFileName, 
                                 characterImage1,data.coordinateX1);
        }
        if (NotNullNorEmpty(data.character2Action))
        {
            UpdateCharacterImage(data.character2Action,data.character2ImageFileName, 
                                 characterImage2,data.coordinateX2);
        }
        currentLine++;
    }

    bool NotNullNorEmpty(string str)
    {
        return !string.IsNullOrEmpty(str);
    }

    void ShowChoices()
    {
        var data = storyData[currentLine];
        choiceButton1.onClick.RemoveAllListeners();
        choiceButton2.onClick.RemoveAllListeners();
        choicePanel.SetActive(true);
        choiceButton1.GetComponentInChildren<TextMeshProUGUI>().text = data.speakingContent;
        choiceButton1.onClick.AddListener(() => InitializeAndLoadStory(data.avatarImageFileName));
        choiceButton2.GetComponentInChildren<TextMeshProUGUI>().text = data.vocalAudioFileName;
        choiceButton2.onClick.AddListener(()=>InitializeAndLoadStory(data.backgroundImageFileName));
    }
    
    void UpdateAvatarImage(string imageFileName)
    {
       var imagePath = Constants.AVATAR_PATH + imageFileName;
        UpdateImage(imagePath,avatarImage);
    }

    void PlayVocalAudio(string audiofilename)
    {
        string audioPath = Constants.VOCAL_PATH + audiofilename;
        PlayAudio(audioPath, vocalAudio, false);
    }

    void UpdateBackgroundImage(string imageFileName)
    {
        string imagePath = Constants.BACKGROUND_PATH + imageFileName;
        UpdateImage(imagePath, backgroundImage);
    }

    void PlayBackgroundMusic(string musicFileName) 
    {
        string musicPath = Constants.MUSIC_PATH + musicFileName;
        PlayAudio(musicPath, backgroundMusic, true);
    }

     void UpdateCharacterImage(string action,string imageFileName,Image characterImage,string x)
     {
        //根据action执行对应的动画操作
        if (action.StartsWith(Constants.APPEAR_AT))
        {
            string imagePath = Constants.CHARACTER_PATH + imageFileName;
            if(NotNullNorEmpty(x))
            {
                UpdateImage(imagePath, characterImage);
                var newPosition = new Vector2(float.Parse(x),characterImage.rectTransform.anchoredPosition.y);
                characterImage.rectTransform.anchoredPosition = newPosition;
                characterImage.DOFade(1, Constants.DURATION_TIME).From(0);
            }
           else
            {
                Debug.LogError(Constants.COORDINATE_MISSING);
            }
        }
        else if (action == Constants.DISAPPEAR) //隐藏角色立绘 添加消失动画
        {
            characterImage.DOFade(0,Constants.DURATION_TIME).OnComplete(()=>characterImage.gameObject.SetActive(false));
        }
        else if (action.StartsWith(Constants.MOVE_TO)) 
        {
            if (NotNullNorEmpty(x))
            {
                characterImage.rectTransform.DOAnchorPosX(float.Parse(x),Constants.DURATION_TIME);
            }
            else
            {
                Debug.LogError(Constants.COORDINATE_MISSING);
            }
        }
     }

    void UpdateImage(string imagePath,Image image)
    {
        Sprite sprite = Resources.Load<Sprite>(imagePath);
        if(sprite != null)
        {
            image.sprite = sprite;
            image.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError(Constants.IMAGE_LOAD_FAILED + imagePath);
        }
    }

   void PlayAudio(string audioPath,AudioSource audioSource,bool isLoop)
    {
        AudioClip audioClip = Resources.Load<AudioClip>(audioPath);
        if (audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();
            audioSource.loop = isLoop;
        }
        else
        {
            Debug.LogError(Constants.AUDIO_LOAD_FAILED + audioPath);

        }
    }

    bool IsHittingBottomButtons()
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            bottomButtons.GetComponent<RectTransform>(),
            Input.mousePosition,
            null );
    }
    

    void OnAutoButtonClick()
    {
        isAutoPlay = !isAutoPlay;
        //UpdateButtonImage((isAutoPlay ? Constants.AUTO_ON : Constants.AUTO_OFF), autoButton);
        if(isAutoPlay)
        {
            StartCoroutine(StartAutoPlay());
        }
    }

    void OnSkipButtonClick()
    {
        if(!isSkip && CanSkip())
       {
            StartSkip();
       }
        else if(isSkip)
        {
            StopCoroutine(SkipToMaxReachedLine());
            EndSkip();
        }
    }

    void OnSaveButtonClick()
    {
        SaveLoadManager.Instance.ShowSaveLoadUI(true);
    }
    void OnLoadButtonClick()
    {
        SaveLoadManager.Instance.ShowSaveLoadUI(false);
    }

    bool CanSkip()
    {
        return currentLine < maxReachedLineIndex;
    }

    void StartSkip()
    {
        isSkip = true;
        //UpdateButtonImage(Constants.SKIP_ON, skipButton);
        typewriteEffect.typingSpeed = Constants.SKIP_MODE_TYPING_SPEED;
        StartCoroutine(SkipToMaxReachedLine());
    }
    void UpdateButtonImage(string imageFileName,Button button)
    {
        string imagePath = Constants.BUTTON_PATH + imageFileName;
        UpdateImage(imagePath, button.image);
    }

    private IEnumerator StartAutoPlay()
    {
        while(isAutoPlay)
        {
            if (!typewriteEffect.IsTyping())
            {
                DisplayNextLine();
            }
            yield return new WaitForSeconds(Constants.DEFAULT_AUTO_WATITING_SECONDS);
        }
    }

    private IEnumerator SkipToMaxReachedLine()
    {
        while (isSkip)
        {
            if (CanSkip())
            {
               DisplayThisLine();
            }
            else
            {
             EndSkip();
            }
            yield return new WaitForSeconds(Constants.DEFAULT_SPIK_WATITING_SECONDS);
        }  
    }
      void EndSkip()
      {
        isSkip = false;
        typewriteEffect.typingSpeed = Constants.DEFAULT_TYPING_SPEED;
        //UpdateButtonImage(Constants.SKIP_OFF, skipButton);

      }

    void OnHomeButtonClick()
    {
        gamePanel.SetActive(false);
        MenuManager.Instance.menuPanel.SetActive(true);
    }

    void OnCloseButtonClick()
    {
        CloseUI();
    }

    void OpenUI()
    {
        dialogueBox.SetActive(true);
        bottomButtons.SetActive(true);
    }

    void CloseUI()
    {
        dialogueBox.SetActive(false);
        bottomButtons.SetActive(false);
    }

}

