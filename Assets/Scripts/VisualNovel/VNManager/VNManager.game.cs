using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNManager
{
    private void Live2DGameScene(ExcelData data)
    {
        string targetSceneName = data.speakingContent;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            targetSceneName = Constants.LIVE2D_GAME_SCENE;
        }

        GameManager.Instance.currentStoryFile = currentStoryFileName;

        // 回来以后不会重复执行 game 这一行，要从下一行继续
        GameManager.Instance.currentLineIndex = currentLine + 1;

        GameManager.Instance.currentScene = Constants.GAME_SCENE;

        SceneManager.LoadScene(targetSceneName);
    }

 
}