using UnityEngine;
using DG.Tweening;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PhoneUIController : MonoBehaviour
{
    [Header("手机整体")]
    [SerializeField] private RectTransform phoneRoot;

    [Header("页面顺序：主页、联系人、背包、任务、状态")]
    [SerializeField] private GameObject[] pages = new GameObject[5];

    [Header("动画时间")]
    [SerializeField] private float openDuration = 0.3f;
    [SerializeField] private float closeDuration = 0.2f;

    private CanvasGroup phoneGroup;
    private Vector2 openPosition;
    private Tween moveTween;

    private bool isOpen;
    private bool initialized;

    private void Start()
    {
        if (phoneRoot == null)
        {
            Debug.LogError("PhoneUIController：请绑定 PhoneRoot。", this);
            enabled = false;
            return;
        }

        // 记录你在编辑器里摆好的位置，作为打开位置。
        Canvas.ForceUpdateCanvases();
        openPosition = phoneRoot.anchoredPosition;

        phoneGroup = phoneRoot.GetComponent<CanvasGroup>();

        if (phoneGroup == null)
            phoneGroup = phoneRoot.gameObject.AddComponent<CanvasGroup>();

        ShowPage(0);

        phoneGroup.interactable = false;
        phoneGroup.blocksRaycasts = false;
        phoneRoot.gameObject.SetActive(false);

        initialized = true;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
            TogglePhone();

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            ClosePhone();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Tab))
            TogglePhone();

        if (Input.GetKeyDown(KeyCode.Escape))
            ClosePhone();
#endif
    }

    // 根据父容器高度，计算屏幕下方的位置。
    private Vector2 GetHiddenPosition()
    {
        RectTransform parent = phoneRoot.parent as RectTransform;

        float parentHeight = parent != null
            ? parent.rect.height
            : phoneRoot.rect.height;

        float distance = parentHeight
            + phoneRoot.rect.height * Mathf.Abs(phoneRoot.localScale.y)
            + 100f;

        return openPosition + Vector2.down * distance;
    }

    public void TogglePhone()
    {
        if (!initialized) return;

        if (isOpen)
            ClosePhone();
        else
            OpenPhone();
    }

    public void OpenPhone()
    {
        if (!initialized || isOpen) return;

        // 中断旧动画，快速连续按键时也能从当前位置继续。
        moveTween?.Kill();
        isOpen = true;

        if (!phoneRoot.gameObject.activeSelf)
        {
            phoneRoot.anchoredPosition = GetHiddenPosition();
            phoneRoot.gameObject.SetActive(true);
            ShowPage(0);
        }

        phoneGroup.interactable = false;
        phoneGroup.blocksRaycasts = true;

        moveTween = phoneRoot
            .DOAnchorPos(openPosition, openDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetRecyclable(false)
            .OnComplete(() =>
            {
                phoneGroup.interactable = true;
            });
    }

    public void ClosePhone()
    {
        if (!initialized || !isOpen) return;

        moveTween?.Kill();
        isOpen = false;
        phoneGroup.interactable = false;

        moveTween = phoneRoot
            .DOAnchorPos(GetHiddenPosition(), closeDuration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .SetRecyclable(false)
            .OnComplete(() =>
            {
                phoneGroup.blocksRaycasts = false;
                phoneRoot.gameObject.SetActive(false);
            });
    }

    public void ShowPage(int index)
    {
        if (index < 0 || index >= pages.Length) return;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == index);
        }
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
    }
}