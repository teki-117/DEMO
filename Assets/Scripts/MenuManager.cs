using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class MenuManager : MonoBehaviour
{
    public GameObject menuPanel;
    public Button startButton;
    public Button continueButton;
    public Button loadButton;
    public Button galleryButton;
    public Button settingsButton;
    public Button quitButton;
    public Button languageButton;
    public TextMeshProUGUI languageButtonText;

    public AudioSource musicAudio;

    private int lastLanguageIndex = Constants.DEFAULT_LANGUAGE_INDEX;
    public int currentLanguageIndex = Constants.DEFAULT_LANGUAGE_INDEX;
    private string currentLanguage;
    private bool hasStarted = false;

    public static MenuManager Instance { get; private set; }

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
        MenuButtonsAddListener();
        LocalizationManager.Instance.LoadLanguage(Constants.DEFAULT_LANGUAGE);
        UpdateLanguageButtonText();
        PlayMainMenuMusic();
    }
    void MenuButtonsAddListener()
    {
        //startButton.onClick.AddListener(StartGame);
        startButton.onClick.AddListener(ShowInputPanel);
        continueButton.onClick.AddListener(ContinueGame);
        loadButton.onClick.AddListener(LoadGame);
        galleryButton.onClick.AddListener(ShowGalleryPanel);
        settingsButton.onClick.AddListener(ShowSettingPanel);
        quitButton.onClick.AddListener(QuitGame);
        languageButton.onClick.AddListener(UpdateLanguage);
    }

    void PlayMainMenuMusic()
    {
        string audioPath = Constants.MUSIC_PATH + Constants.MAIN_MENU_MUSIC_FILE_NAME;
        PlayAudio(audioPath, musicAudio, true);
    }
    void PlayAudio(string audioPath, AudioSource audioSource, bool isLoop)
    {
        AudioClip audioClip = Resources.Load<AudioClip>(audioPath);

        if (audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.loop = isLoop;
            audioSource.gameObject.SetActive(true);
            audioSource.Play();
        }
        else
        {
            Debug.LogError(Constants.AUDIO_LOAD_FAILED + audioPath);
        }
    }
    public void StartGame()
    {
        hasStarted = true;
        VNManager.Instance.StartGame(Constants.DEFAULT_STORY_FILE_NAME, Constants.DEFAULT_START_LINE);
        ShowGamePanel();
    }
    private void ContinueGame()
    {
        if (hasStarted)
        {
            if (lastLanguageIndex != currentLanguageIndex)
            {
                VNManager.Instance.ReloadStoryLine();
            }
            ShowGamePanel();
        }
    }
    private void LoadGame()
    {
        VNManager.Instance.ShowLoadPanel(ShowGamePanel);
    }
    private void ShowInputPanel()
    {
        InputManager.Instance.ShowInputPanel();
    }    
    private void ShowGamePanel()
    {
        menuPanel.SetActive(false);
        VNManager.Instance.gamePanel.SetActive(true);
    }
    private void ShowGalleryPanel()
    {
        GalleryManager.Instance.ShowGalleryPanel();
    }
    private void ShowSettingPanel()
    {
        SettingManager.Instance.ShowSettingPanel();
    }
    private void QuitGame()
    {
        Application.Quit();
    }
    private void UpdateLanguage()
    {
        currentLanguageIndex = (currentLanguageIndex + 1) % Constants.LANGUAGES.Length;

        currentLanguage = Constants.LANGUAGES[currentLanguageIndex];
        LocalizationManager.Instance.LoadLanguage(currentLanguage);
        UpdateLanguageButtonText();
    }
    void UpdateLanguageButtonText()
    {
        switch (currentLanguageIndex)
        {
            case 0:
                languageButtonText.text = Constants.CHINESE;
                break;

            case 1:
                languageButtonText.text = Constants.ENGLISH;
                break;

            case 2:
                languageButtonText.text = Constants.JAPANESE;
                break;
        }
    }
}