using UnityEngine;

public class Parallax : MonoBehaviour
{
    private Transform _mainCamera;      // 主相机Tf
    private Vector3 _lastPosition;      // 相机位置
    [SerializeField] private float speed = 1f;   // 视差效果参数

    private void Start()
    {
        _mainCamera = Camera.main.transform;    // 获取相机
        _lastPosition = _mainCamera.position;   // 相机位置
    }

    private void LateUpdate()
    {
        ParallaxMove();     // 视差移动
    }

    private void ParallaxMove()
    {
        float deltaX = _mainCamera.position.x - _lastPosition.x;     // deltaX：相机在过去一帧移动的距离
        transform.position += new Vector3(deltaX * speed, 0, 0);    // 更新背景位置
        _lastPosition = _mainCamera.position;   // 更新
    }
}