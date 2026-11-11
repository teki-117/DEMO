using DG.Tweening;
using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Rendering;
using UnityEngine;

public class Live2DCharacterDisplay : MonoBehaviour, ICharacterDisplay
{
    private GameObject _modelInstance;
    private CubismMotionController _motionCtrl;
    private CubismExpressionController _exprCtrl;
    private CharacterData _data;

    public new GameObject gameObject => base.gameObject;

    public Transform RootTransform => transform;

    public void Setup(CharacterData data, Vector2 pos, Vector2 scale)
    {
        bool needRebuild = (_data != data) || (_modelInstance == null);
        _data = data;

        if (_data == null || _data.live2DModelPrefab == null)
        {
            gameObject.SetActive(false);
            return;
        }
        if (needRebuild)
        {
            if (_modelInstance != null)
                Destroy(_modelInstance);

            _modelInstance = Instantiate(_data.live2DModelPrefab, transform);
            _modelInstance.name = _data.characterID;

            _motionCtrl = _modelInstance.GetComponent<CubismMotionController>();
            _exprCtrl = _modelInstance.GetComponent<CubismExpressionController>();

            _modelInstance.transform.localPosition = Vector3.zero;
            _modelInstance.transform.localScale = Vector3.one;

            ApplyCubismSorting(_modelInstance, Constants.L2D_DEFAULT_LAYER, Constants.L2D_DEFAULT_ORDER_IN_LAYER);
        }

        var rt = (RectTransform)transform;
        rt.anchorMin = Constants.CENTER;
        rt.anchorMax = Constants.CENTER;
        rt.pivot = Constants.CENTER;
        rt.anchoredPosition = pos;
        rt.localScale = scale;

        gameObject.SetActive(true);
    }
    public void SetExpression(string key)
    {
        if (_exprCtrl == null || _data == null) return;

        if (_data.TryGetLive2DExpressionIndex(key, out var idx))
            _exprCtrl.CurrentExpressionIndex = idx;
    }

    public void PlayMotion(string key, bool loop = false)
    {
        if (_motionCtrl == null || _data == null) return;

        if (_data.TryGetLive2DMotion(key, out var clip))
            _motionCtrl.PlayAnimation(clip, isLoop: loop);
    }


    public void MoveToX(float x, float duration = 0.2f)
    {
        var rt = (RectTransform)transform;

        rt.DOKill();
        rt.DOAnchorPosX(x, duration);
    }

    public void Hide() => gameObject.SetActive(false);

    private static void ApplyCubismSorting(
        GameObject modelRoot,
        string sortingLayerName,
        int orderInLayer)
    {
        var rc = modelRoot.GetComponent<CubismRenderController>();

        if (rc != null)
        {
            rc.SortingLayer = sortingLayerName;
            rc.SortingOrder = orderInLayer;
            rc.SortingMode = CubismSortingMode.BackToFrontOrder;
        }
    }
}