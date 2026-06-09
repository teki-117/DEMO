using System.Collections.Generic;
using UnityEngine;

public class CharacterStateManager : MonoBehaviour
{
    public CharacterData[] allCharacterDatas;
    private Dictionary<string, CharacterState> states;

    public static CharacterStateManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        states = new Dictionary<string, CharacterState>();
        foreach (var cd in allCharacterDatas)
        {
            if (!states.ContainsKey(cd.characterID))
            {
                states[cd.characterID] = new CharacterState(cd.characterID);
            }
        }
    }

    public int GetAffection(string characterID)
    {
        return states.TryGetValue(characterID, out var s) ? s.affection : 0;
    }

    public void ChangeAffection(string characterID, int delta)
    {
        if (!states.TryGetValue(characterID, out var s))
        {
            Debug.LogWarning($"无效的角色 ID: {characterID}");
        }
        s.affection += delta;
    }
    public void ResetAffection()
    {
        foreach (var kv in states)
        {
            kv.Value.affection = 0;
        }
    }

    public void LoadStates(Dictionary<string, int> map)
    {
        foreach (var kv in map)
        {
            if (states.ContainsKey(kv.Key))
            {
                states[kv.Key].affection = kv.Value;
            }
        }
    }

    public Dictionary<string, int> DumpStates()
    {
        var map = new Dictionary<string, int>();
        foreach (var kv in states)
        {
            map[kv.Key] = kv.Value.affection;
        }
        return map;
    }
}
