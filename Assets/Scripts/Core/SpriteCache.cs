using System.Collections.Generic;
using UnityEngine;

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