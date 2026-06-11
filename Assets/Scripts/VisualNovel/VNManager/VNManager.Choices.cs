using System;
using System.Collections.Generic;
using System.Linq;

public partial class VNManager
{
    private void Choices_Show()
    {
        var choices = Choices_ParseCurrent();
        ChoiceManager.Instance.ShowChoices(choices, Choices_HandleSelect);
    }

    private void Choices_HandleSelect(string selected)
    {
        currentLine = Constants.DEFAULT_START_LINE;

        //if (Constants.ALL_MEMORIES.Contains(selected))
        //    GameManager.Instance.unlockedMemories.Add(selected);

        Flow_LoadStory(selected);
        Flow_Next();
    }
    private List<ChoiceOption> Choices_ParseCurrent()
    {
        var data = storyData[currentLine];

        string[] choiceTexts = LM.GetSpeakingContent(data)
            .Split(Constants.ChoiceDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray();

        string[] targets = (data.avatarImageFileName ?? "")
            .Split(Constants.ChoiceDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray();

        string[] changes = (data.vocalAudioFileName ?? "")
            .Split(Constants.ChoiceDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray();

        string[] conditions = (data.backgroundImageFileName ?? "")
            .Split(Constants.ChoiceDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray();

        var list = new List<ChoiceOption>();
        int n = choiceTexts.Length;
        for (int i = 0; i < n; i++)
        {
            list.Add(new ChoiceOption
            {
                text = choiceTexts.ElementAtOrDefault(i) ?? "",
                nextStoryFileName = targets.ElementAtOrDefault(i) ?? "",
                changes = ParseChanges(changes.ElementAtOrDefault(i)),
                conditions = ParseConditions(conditions.ElementAtOrDefault(i))
            });
        }
        return list;
    }
    // "Alice:+10;Bob:-5"
    private List<AffectionChange> ParseChanges(string raw)
    {
        var list = new List<AffectionChange>();
        if (string.IsNullOrEmpty(raw)) return list;

        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length != 2) continue;
            if (int.TryParse(kv[1], out int delta))
                list.Add(new AffectionChange { characterID = kv[0].Trim(), delta = delta });
        }

        return list;
    }

    // "Annie:0,100;Bob:-50,50"
    private List<AffectionCondition> ParseConditions(string raw)
    {
        var list = new List<AffectionCondition>();
        if (string.IsNullOrEmpty(raw)) return list;

        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length != 2) continue;

            var id = kv[0].Trim();
            var nums = kv[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (nums.Length != 2) continue;

            if (int.TryParse(nums[0], out int min) && int.TryParse(nums[1], out int max))
                list.Add(new AffectionCondition { characterID = id, minValue = min, maxValue = max });
        }

        return list;
    }
}