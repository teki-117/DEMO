using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GalleryManager : MonoBehaviour
{
    public GameObject galleryPanel;
    public TextMeshProUGUI panelTitle;
    public Button[] galleryButtons;
    public Button prevPageButton;
    public Button nextPageButton;
    public Button backButton;
    public GameObject bigImagePanel; // 全屏大图面板
    public Image bigImage;           // 显示大图的 Image

    private int currentPage = Constants.DEFAULT_START_INDEX;
    private readonly int slotsPerPage = Constants.GALLERY_SLOTS_PER_PAGE;
    private readonly int totalSlots = Constants.ALL_BACKGROUNDS.Length;

    public static GalleryManager Instance{ get;private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        prevPageButton.onClick.AddListener(PrevPage);
        nextPageButton.onClick.AddListener(NextPage);
        backButton.onClick.AddListener(GoBack);

        galleryPanel.SetActive(false);
        bigImagePanel.SetActive(false);

        Button bigImageButton = bigImagePanel.GetComponent<Button>();
        if (bigImageButton != null)
        {
            bigImageButton.onClick.AddListener(HideBigImage);
        }
    }

    public void ShowGalleryPanel()
    {
        panelTitle.text = GetLocalized(Constants.GALLERY);
        prevPageButton.GetComponentInChildren<TextMeshProUGUI>().text = GetLocalized(Constants.PREV_PAGE);
        nextPageButton.GetComponentInChildren<TextMeshProUGUI>().text = GetLocalized(Constants.NEXT_PAGE);
        backButton.GetComponentInChildren<TextMeshProUGUI>().text = GetLocalized(Constants.BACK);

        UpdateUI();
        galleryPanel.SetActive(true);
    }
    private void UpdateUI()
    {
        for (int i = 0; i < slotsPerPage; ++i)
        {
            int slotIndex = currentPage * slotsPerPage + i;
            if (slotIndex < totalSlots)
            {
                UpdateGalleryButtons(galleryButtons[i],slotIndex);
            }
            else
            {
                galleryButtons[i].gameObject.SetActive(false);
            }
        }
    }
    private void UpdateGalleryButtons(Button button, int index)
    {
        button.gameObject.SetActive(true);
        string bgName = Constants.ALL_BACKGROUNDS[index];
        bool isUnlocked = VNManager.Instance.unlockedBackgrounds.Contains(bgName);

        string imagePath = Constants.THUMBNAIL_PATH + (isUnlocked ? bgName : Constants.GALLERY_PLACEHOLDER);
        Sprite sprite = Resources.Load<Sprite>(imagePath);
        if (sprite != null)
        {
            button.GetComponentInChildren<Image>().sprite = sprite;
        }
        else
        {
            Debug.LogError(Constants.IMAGE_LOAD_FAILED + imagePath);
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnButtonClick(button, index));
    }
    private void OnButtonClick(Button button, int index)
    {
        string bgName = Constants.ALL_BACKGROUNDS[index];
        bool isUnlocked = VNManager.Instance.unlockedBackgrounds.Contains(bgName);

        if (!isUnlocked)
        {
            return;
        }

        string imagePath = Constants.BACKGROUND_PATH + bgName;
        Sprite sprite = Resources.Load<Sprite>(imagePath);

        if (sprite != null)
        {
            bigImage.sprite = sprite;
            bigImagePanel.SetActive(true);
        }
        else
        {
            Debug.LogError(Constants.BIG_IMAGE_LOAD_FAILED + imagePath);
        }
    }
    private void HideBigImage()
    {
        bigImagePanel.SetActive(false);
    }
    private void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateUI();
        }
    }
    private void NextPage()
    {
        if ((currentPage + 1) * slotsPerPage < totalSlots)
        {
            currentPage++;
            UpdateUI();
        }
    }
    private void GoBack()
    {
        galleryPanel.SetActive(false);
    }
    string GetLocalized(string key)
    {
        return LocalizationManager.Instance.GetLocalizedValue(key);
    }
}