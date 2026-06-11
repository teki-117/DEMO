using UnityEngine;
using UnityEngine.EventSystems;

public partial class VNManager
{
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
}