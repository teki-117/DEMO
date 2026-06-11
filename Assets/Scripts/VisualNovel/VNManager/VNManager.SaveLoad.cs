using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public partial class VNManager
{
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
            savedAffection = CharacterStateManager.Instance.DumpStates()
        };
    }
}