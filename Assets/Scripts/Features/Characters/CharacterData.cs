using System;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterRenderKind { Sprite,Live2D }

[CreateAssetMenu(menuName = "VN/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterID;

    [Header("联系人资料")]
    public string displayName;
    public Sprite contactAvatar;

    [Header("Render Kind")]
    public CharacterRenderKind renderKind = CharacterRenderKind.Sprite;

    [Header("Sprite Mode")]
    public Sprite standSprite;

    [Serializable]
    public class Expression
    {
        public string key;
        public Sprite sprite;
    }
    public List<Expression> expressions = new();
    public Dictionary<string, Sprite> expressionMap;

    [Header("Live2D Mode")]
    public GameObject live2DModelPrefab;

    [Serializable]
    public class Live2DMotion
    {
        public string key;
        public AnimationClip clip;
    }
    public List<Live2DMotion> live2DMotions = new();
    public Dictionary<string, AnimationClip> live2DMotionMap;

    [Serializable]
    public class Live2DExpression
    {
        public string key;
        public int index;
    }
    public List<Live2DExpression> live2DExpressions = new();
    public Dictionary<string, int> live2DExpressionMap;

    [Header("RPG 初始属性")]
    [Min(1)] public int startingLevel = 1;
    [Min(1)] public int startingMaxHP = 100;
    [Min(0)] public int startingMaxSP = 30;
    [Min(0)] public int startingAttack = 10;
    [Min(0)] public int startingDefense = 5;
    private void OnEnable()
    {
        // Sprite expressions
        expressionMap = new Dictionary<string, Sprite>();
        foreach (var expr in expressions)
        {
            if (string.IsNullOrEmpty(expr?.key) || expr.sprite == null) continue;
            expressionMap[expr.key] = expr.sprite; // 后写覆盖，便于修配置
        }

        // Live2D motion map
        live2DMotionMap = new Dictionary<string, AnimationClip>();
        foreach (var m in live2DMotions)
        {
            if (string.IsNullOrEmpty(m?.key) || m.clip == null) continue;
            live2DMotionMap[m.key] = m.clip;
        }

        // Live2D expression map
        live2DExpressionMap = new Dictionary<string, int>();

        foreach (var e in live2DExpressions)
        {
            if (string.IsNullOrEmpty(e?.key)) continue;
            live2DExpressionMap[e.key] = e.index;
        }
    }
    public bool TryGetLive2DMotion(string key, out AnimationClip clip)
    {
        clip = null;
        return live2DMotionMap != null && live2DMotionMap.TryGetValue(key, out clip);
    }

    public bool TryGetLive2DExpressionIndex(string key, out int idx)
    {
        idx = -1;
        return live2DExpressionMap != null && live2DExpressionMap.TryGetValue(key, out idx);
    }
}
