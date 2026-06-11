using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SpriteCharacterDisplay : MonoBehaviour, ICharacterDisplay
{
    [SerializeField] private Image image;

    public new GameObject gameObject => base.gameObject;

    public Transform RootTransform => transform;

    private CharacterData _data;

    public void Setup(CharacterData data, Vector2 pos, Vector2 scale)
    {
        _data = data;
        image.sprite = data.standSprite;

        RectTransform rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = pos;
        rt.localScale = scale;

        gameObject.SetActive(true);
    }
    public void SetExpression(string key)
    {
        if (_data != null && _data.expressionMap != null &&
            _data.expressionMap.TryGetValue(key, out var sp))
        {
            image.sprite = sp;
        }
    }

    public void PlayMotion(string key, bool loop = false)
    {
    }

    public void MoveToX(float x, float duration = 0.2f)
    {
        var rt = (RectTransform)transform;

        rt.DOKill();
        rt.DOAnchorPosX(x, duration);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}