using UnityEngine;
using UnityEngine.SceneManagement;

public class MuSceneController : MonoBehaviour
{
    public void ExitToGameScene()
    {
        Debug.Log("点击了出门按钮，准备返回 GameScene");
        SceneManager.LoadScene(Constants.GAME_SCENE);
    }
}