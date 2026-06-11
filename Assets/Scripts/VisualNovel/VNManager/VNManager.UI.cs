using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNManager
{
    private void UI_AddListeners()
    {
        autoButton.onClick.AddListener(() =>
        {
            // ÇÐ»» Auto <-> Manual
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
}