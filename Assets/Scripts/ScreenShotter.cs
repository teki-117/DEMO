using UnityEngine;

public class ScreenShotter : MonoBehaviour
{
 public Texture2D CaptureScreenshot()
    {
        int width = Screen.width;
        int height = Screen.height;

        RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);

        Camera mainCamera = Camera.main;

        if (mainCamera == null )
        {
            Debug.LogError(Constants.CAMERA_NOT_FOUND);
            return null;
        }
        mainCamera.targetTexture = rt;
        RenderTexture.active = rt;
        mainCamera.Render();

        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        mainCamera.targetTexture = null;
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        Texture2D resizedScreenshot = ResizeTexture(screenshot, width / 6, height / 6);

        Destroy(screenshot);
        return resizedScreenshot;
    }

    private Texture2D ResizeTexture(Texture2D original, int newWidth, int newHeight)
    {
        /*步骤 1：创建渲染纹理
        创建一个与目标分辨率相匹配的渲染纹理，并激活*/
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 24);
        RenderTexture.active = rt;

        /*步骤 2：使用 GPU 缩放
        使用 GPU 的 Graphics.Blit 将 original 的像素数据拷贝并缩放到 rt
        为什么使用 GPU？
        GPU 操作比手动逐像素缩放效率更高，适合实时操作*/
        Graphics.Blit(original, rt);

        /*步骤 3：读取缩放后的数据
        读取 RenderTexture 的内容到新的 Texture2D，与截图的逻辑类似*/
        Texture2D resized = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        resized.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resized.Apply();

        /*步骤 4：释放资源
        清理临时 RenderTexture，避免内存泄漏*/
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return resized;
    }

}
