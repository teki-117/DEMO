using UnityEngine;
using UnityEngine.SceneManagement;

public class MuSceneController : MonoBehaviour
{
    public void ExitToGameScene()
    {
        SceneManager.LoadScene(Constants.GAME_SCENE);
    }
}