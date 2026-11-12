using System.Collections.Generic;
using UnityEngine;

// 横向无限循环的背景。挂在同一层的每块背景图上（同一个父物体下）。
// 同一层里排在最上面的那块负责全部工作：
// 1. 按图片的真实宽度把各块重新排齐，不留缝；
// 2. 块数不够铺满镜头时自动复制；
// 3. 相机走到哪，就把离得远的块挪到另一侧。
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScroller : MonoBehaviour
{
    [Tooltip("相邻两块重叠多少（世界单位），用来盖住接缝。")]
    [SerializeField] private float overlap = 0.02f;

    private Camera mainCamera;
    private Transform layer;                       // 父物体（视差层）
    private readonly List<Transform> tiles = new List<Transform>();
    private float step;                            // 相邻两块的间距（父物体的本地单位）
    private float anchorX;                         // 第一块的初始位置（本地）
    private bool isLeader;

    private void Start()
    {
        mainCamera = Camera.main;
        layer = transform.parent;

        if (mainCamera == null || layer == null)
        {
            Debug.LogWarning("BackgroundScroller 需要主相机，并且背景图要放在一个父物体下面。", this);
            enabled = false;
            return;
        }

        // 同一层里只让排在最上面的那块干活，其余的只当图片用。
        BackgroundScroller leader = null;

        foreach (Transform child in layer)
        {
            BackgroundScroller s = child.GetComponent<BackgroundScroller>();

            if (s == null)
                continue;

            if (leader == null)
                leader = s;

            if (leader == this)
                tiles.Add(child);
        }

        isLeader = leader == this;

        if (!isLeader)
        {
            enabled = false;
            return;
        }

        float layerScale = Mathf.Max(0.0001f, Mathf.Abs(layer.lossyScale.x));

        // 图片的真实宽度（父物体的本地单位）。
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float tileWidth = sr.sprite.bounds.size.x * Mathf.Abs(transform.localScale.x);

        step = tileWidth - overlap / layerScale;
        anchorX = transform.localPosition.x;

        // 镜头宽度（本地单位）。块数要保证：挪走一块时，剩下的仍能盖满镜头。
        float viewWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect / layerScale;
        int needed = Mathf.CeilToInt(viewWidth / step) + 2;

        while (tiles.Count < needed)
        {
            GameObject copy = Instantiate(gameObject, layer);
            copy.name = name + "_copy" + tiles.Count;

            // 复制出来的块不需要再跑这个脚本。
            BackgroundScroller copyScroller = copy.GetComponent<BackgroundScroller>();
            copyScroller.enabled = false;
            Destroy(copyScroller);

            tiles.Add(copy.transform);
        }

        Arrange();
    }

    private void LateUpdate()
    {
        if (isLeader)
            Arrange();
    }

    // 每块都放在“离相机最近的那个循环位置”上，间距永远是 step，所以不会出缝。
    private void Arrange()
    {
        float cameraX = layer.InverseTransformPoint(mainCamera.transform.position).x;
        float loop = step * tiles.Count;

        for (int i = 0; i < tiles.Count; i++)
        {
            Transform tile = tiles[i];

            if (tile == null)
                continue;

            float baseX = anchorX + i * step;
            float x = baseX + Mathf.Round((cameraX - baseX) / loop) * loop;

            Vector3 p = tile.localPosition;
            p.x = x;
            tile.localPosition = p;
        }
    }
}
