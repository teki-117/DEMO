using UnityEngine;
using UnityEngine.SceneManagement;

public class MuSceneController : MonoBehaviour
{
    // 出门按钮。从地图的家进来时回地图并推进时段；从剧情里进来时回剧情。
    public void ExitToGameScene()
    {
        if (WorldManager.Instance != null && WorldManager.Instance.LeaveHome())
            return;

        SceneManager.LoadScene(Constants.GAME_SCENE);
    }
}