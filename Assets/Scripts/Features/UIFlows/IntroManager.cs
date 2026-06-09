using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    private string videoPath = "video/1.mp4";
    private bool hasChangedScene = false;

    void Start()
    {
        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoPath);
        videoPlayer.url = fullPath; // 直接加载路径
        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.Play();
    }

    void Update()
    {
        if (hasChangedScene)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SkipIntro();
        }
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        LoadMainGameScene();
    }

    void SkipIntro()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        LoadMainGameScene();
    }

    void LoadMainGameScene()
    {
        if (hasChangedScene)
        {
            return;
        }

        hasChangedScene = true;
        SceneManager.LoadScene("MenuScene");
    }
}