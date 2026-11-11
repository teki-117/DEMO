using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("Containers")]
    public RectTransform spriteContainer;
    public RectTransform live2DContainer;

    [Header("Prefabs")]
    public SpriteCharacterDisplay spriteDisplayPrefab;
    public Live2DCharacterDisplay live2DDisplayPrefab;

    [Header("Characters Data")]
    public CharacterData[] allCharacters;

    private readonly List<ICharacterDisplay> pool = new();
    private readonly Dictionary<string, ICharacterDisplay> active = new();

    public void ShowCharacter(
    string characterID,
    Vector2? position = null,
    Vector2? scale = null,
    string expressionKey = null,
    string motionKey = null,
    bool motionLoop = true)
    {
        var data = allCharacters.FirstOrDefault(c => c.characterID == characterID);
        if (data == null) return;

        var pos = position ?? Vector2.zero;
        var sca = scale ?? Vector2.one;

        if (!active.TryGetValue(characterID, out var display))
        {
            display = GetDisplay(data);
            active[characterID] = display;
        }
        display.Setup(data, pos, sca);

        if (!string.IsNullOrEmpty(expressionKey))
            display.SetExpression(expressionKey);

        if (data.renderKind == CharacterRenderKind.Live2D)
        {
            if (!string.IsNullOrEmpty(motionKey))
                display.PlayMotion(motionKey, motionLoop);
            else
                display.PlayMotion(Constants.L2D_DEFAULT_MOTION, motionLoop);
        }
    }
    private ICharacterDisplay GetDisplay(CharacterData data)
    {
        foreach (var d in pool)
        {
            if (!d.gameObject.activeSelf)
            {
                if (data.renderKind == CharacterRenderKind.Sprite && d is SpriteCharacterDisplay) return d;
                if (data.renderKind == CharacterRenderKind.Live2D && d is Live2DCharacterDisplay) return d;
            }
        }

        if (data.renderKind == CharacterRenderKind.Sprite)
        {
            var inst = Instantiate(spriteDisplayPrefab, spriteContainer);
            pool.Add(inst);
            return inst;
        }
        else
        {
            var inst = Instantiate(live2DDisplayPrefab, live2DContainer);
            pool.Add(inst);
            return inst;
        }
    }
    public void HideCharacter(string characterID)
    {
        if (active.TryGetValue(characterID, out var display))
        {
            display.Hide();
            active.Remove(characterID);
        }
    }
    public void MoveCharacter(string characterID, float x, float duration = 0.2f)
    {
        if (active.TryGetValue(characterID, out var display))
            display.MoveToX(x, duration);
    }

    public void PlayMotion(string characterID, string motionKey, bool loop = false)
    {
        if (active.TryGetValue(characterID, out var display))
            display.PlayMotion(motionKey, loop);
    }

    public void ClearAll()
    {
        foreach (var kv in active) kv.Value.Hide();
        active.Clear();
    }

}

public interface ICharacterDisplay
{
    GameObject gameObject { get; }

    Transform RootTransform { get; }

    void Setup(CharacterData data, Vector2 anchoredPos, Vector2 scale);

    void SetExpression(string key);

    void PlayMotion(string key, bool loop = false);

    void MoveToX(float x, float duration = 0.2f);

    void Hide();
}

public static class SpriteCache
{
    private static readonly Dictionary<string, Sprite> cache = new();

    public static Sprite Get(string path)
    {
        if (cache.TryGetValue(path, out var s) && s != null) return s;

        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null) { Debug.LogError($"{Constants.IMAGE_LOAD_FAILED}{path}"); return null; }

        cache[path] = sprite; return sprite;
    }
}
