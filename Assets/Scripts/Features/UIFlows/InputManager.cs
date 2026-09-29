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
        GameManager.Instance.playerName = playerName;
        SceneManager.LoadScene("MapScene");
    }
    bool IsInvalidName(string name)
    {
        return string.IsNullOrEmpty(name);
    }
}