using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceManager : MonoBehaviour
{
    public GameObject choicePanel;
    public Button choiceButtonPrefab;
    public Transform choiceButtonContainer;
    public static ChoiceManager Instance { get; private set; }
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
        choicePanel.SetActive(false);
    }
    public void ShowChoices(List<ChoiceOption> options, Action<string> onChoiceSelected)
    {
        var available = options.Where(
            opt => opt.conditions.All(
                c =>
                {
                    int aff = CharacterStateManager.Instance.GetAffection(c.characterID);
                    return aff >= c.minValue && aff <= c.maxValue;
                }
            )
        ).ToList();
        if (available.Count == 0)
        {
            return;
        }
        if (available.Count == 1)
        {
            foreach (var ch in available[0].changes)
            {
                CharacterStateManager.Instance.ChangeAffection(ch.characterID, ch.delta);
            }
            onChoiceSelected?.Invoke(available[0].nextStoryFileName);
            return;
        }
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var opt in available)
        {
            var btn = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = opt.text;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                // 点击时先改好感度
                foreach (var ch in opt.changes)
                {
                    CharacterStateManager.Instance.ChangeAffection(ch.characterID, ch.delta);
                }
                onChoiceSelected?.Invoke(opt.nextStoryFileName);
                choicePanel.SetActive(false);
            });
        }
        choicePanel.SetActive(true);
    }
}
