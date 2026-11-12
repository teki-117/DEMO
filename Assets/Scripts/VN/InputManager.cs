using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public TextMeshProUGUI promptText;
    public TMP_InputField nameInputField;
    public Button confirmButton;

    public static InputManager Instance { get; private set; }

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
        promptText.text = LM.GLV(Constants.PROMPT_TEXT);
        nameInputField.text = "";
        confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = LM.GLV(Constants.CONFIRM);
        confirmButton.onClick.AddListener(OnConfirm);
    }
    void OnConfirm()
    {
        string playerName = nameInputField.text.Trim();

        if (IsInvalidName(playerName))
        {
            // 可以添加错误提示（比如用对话框提示玩家）
            return;
        }
        GameManager gm = GameManager.Instance;
        gm.playerName = playerName;

        // 新游戏先强制播剧情 1，播完回地图。开场剧情不推进时间。
        gm.PrepareStory(Constants.DEFAULT_STORY_FILE);
        gm.storyReturnScene = Constants.MAP_SCENE;
        SceneManager.LoadScene(Constants.GAME_SCENE);
    }
    bool IsInvalidName(string name)
    {
        return string.IsNullOrEmpty(name);
    }
}