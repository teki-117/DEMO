using UnityEngine;

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