using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class VNManager : MonoBehaviour
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
  
}