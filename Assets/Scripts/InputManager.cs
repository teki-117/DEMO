using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public GameObject inputPanel;
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
        confirmButton.onClick.AddListener(OnConfirm);
        inputPanel.SetActive(false);
    }

    void OnConfirm()
    {
        string playerName = nameInputField.text.Trim();

        if (IsInvalidName(playerName))
        {
            // 可以添加错误提示（比如用对话框提示玩家）
            return;
        }

        PlayerData.Instance.playerName = playerName;
        inputPanel.SetActive(false);
        MenuManager.Instance.StartGame();
    }

    bool IsInvalidName(string name)
    {
        return string.IsNullOrEmpty(name);
    }
    public void ShowInputPanel()
    {
        confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = GetLocalized(Constants.CONFIRM);
        promptText.text = GetLocalized(Constants.PROMPT_TEXT);
        nameInputField.text = "";
        inputPanel.SetActive(true);
    }
    string GetLocalized(string key)
    {
        return LocalizationManager.Instance.GetLocalizedValue(key);
    }
}