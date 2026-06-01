using UnityEngine;
using System.Collections;

public class FadeInEffect : MonoBehaviour
{
    public CanvasGroup blackOverlay;
    public float fadeDuration = 2.0f;

    void Start()
    {
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        float time = 0;

        while (time < fadeDuration)
        {
            blackOverlay.alpha = 1 - (time / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }

        blackOverlay.alpha = 0;
        blackOverlay.gameObject.SetActive(false);
    }
}