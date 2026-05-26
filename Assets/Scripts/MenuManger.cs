using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class MenuManager : MonoBehaviour
{
    public GameObject menuPanel;
    public Button startButton;
    public Button continueButton;
    public Button loadButton;
    public Button settingsButton;
    public Button quitButton;

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
        menuButtonsAddListener();
    }

    void menuButtonsAddListener()
    {
        startButton.onClick.AddListener(StartGame);
        continueButton.onClick.AddListener(ContinueGame);
    }

    private void StartGame()
    {
        hasStarted = true;
        VNManager.Instance.StartGame();
        menuPanel.SetActive(false);
        VNManager.Instance.gamePanel.SetActive(true);
    }

    private void ContinueGame()
    {
        if (hasStarted)
        {
            menuPanel.SetActive(false);
            VNManager.Instance.gamePanel.SetActive(true);
        }
    }
}