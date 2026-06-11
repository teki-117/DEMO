using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class VNManager
{
    private void Render_InitializeImages()
    {
        backgroundImage.gameObject.SetActive(false);
        avatarImage.gameObject.SetActive(false);
    }

    internal void Flow_LoadStory(string fileName)
    {
        currentStoryFileName = fileName;

        var path = System.IO.Path.Combine(
            Application.streamingAssetsPath,
            Constants.STORY_PATH,
            fileName + Constants.STORY_FILE_EXTENSION
        );

        storyData = ExcelReader.ReadExcel(path);

        if (storyData == null || storyData.Count == 0)
        {
            Debug.LogError(Constants.NO_DATA_FOUND);
        }
        GameManager.Instance.currentStoryFile = currentStoryFileName;

        // ×îÔ¶ÐÐË÷Òý
        var dict = GameManager.Instance.maxReachedLineIndices;
        if (dict.TryGetValue(currentStoryFileName, out var idx))
            maxReachedLineIndex = idx;
        else
        {
            maxReachedLineIndex = 0;
            dict[currentStoryFileName] = 0;
        }

        // »Ö¸´±³¾°¡¢BGM¡¢½ÇÉ«
        Render_RecoverBG_BGM_Characters();
    }
    private void Render_RecoverBG_BGM_Characters()
    {
        var gm = GameManager.Instance;

        if (!string.IsNullOrEmpty(gm.currentBackgroundImg))
            Render_UpdateBackground(gm.currentBackgroundImg);

        if (!string.IsNullOrEmpty(gm.currentBackgroundMusic))
            Audio_PlayBGM(gm.currentBackgroundMusic);

        CharacterManager.Instance.ClearAll();
        foreach (var c in gm.currentCharacterData)
        {
            charStates[c.characterID] = c;
            CharacterManager.Instance.ShowCharacter(
                c.characterID,
                new Vector2(c.positionX, 0f),
                new Vector2(c.scale, c.scale),
                c.expressionName
            );
        }
    }
    private void Render_UpdateAvatar(string fileName)
    {
        var path = Constants.AVATAR_PATH + fileName;
        Render_UpdateImage(path, avatarImage, doFadeIn: false);
    }

    private void Render_UpdateBackground(string fileName)
    {
        var path = Constants.BACKGROUND_PATH + fileName;
        Render_UpdateImage(path, backgroundImage, doFadeIn: false);

        var set = GameManager.Instance.unlockedBackgrounds;
        if (!set.Contains(fileName)) set.Add(fileName);
    }

    private void Render_UpdateImage(string resourcePath, Image target, bool doFadeIn)
    {
        var sprite = SpriteCache.Get(resourcePath);
        if (sprite == null) return;

        target.sprite = sprite;
        target.gameObject.SetActive(true);
        
        if (doFadeIn)
        {
            var cg = target.canvasRenderer;
            target.DOKill();
            target.DOFade(1f, Constants.DURATION_TIME).From(0f).SetId(this);
        }
    }
}









