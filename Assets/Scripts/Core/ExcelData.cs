using System.Collections.Generic;

public class AffectionChange
{
    public string characterID;
    public int delta;
}
public class AffectionCondition
{
    public string characterID;
    public int minValue;
    public int maxValue;
}
public class ChoiceOption
{
    public string text;
    public string nextStoryFileName;
    public List<AffectionChange> changes;
    public List<AffectionCondition> conditions;
}
public class CharacterCommand
{
    public string characterID;
    public string action;
    public float positionX;
    public string expressionName;
    public float scale;
    public string motionKey;
}
public class ExcelData
{
    public string speakerName;
    public string speakingContent;
    public string avatarImageFileName;
    public string vocalAudioFileName;
    public string backgroundImageFileName;
    public string backgroundMusicFileName;
    public List<CharacterCommand> characterCommands;
    public string englishName;
    public string englishContent;
    public string japaneseName;
    public string japaneseContent;
}