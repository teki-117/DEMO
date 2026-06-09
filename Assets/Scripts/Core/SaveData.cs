using System.Collections.Generic;

public class CharacterSaveData
{
    public string characterID;
    public float positionX;
    public string expressionName;
}

public class SaveData
{
    public string savedStoryFileName;
    public int savedLine;
    public byte[] savedScreenshotData;
    public LinkedList<ExcelData> savedHistoryRecords;
    public string savedBackgroundImg;
    public string savedBackgroundMusic;
    public List<CharacterSaveData> savedCharacters;
    public string savedPlayerName;
    public Dictionary<string, int> savedAffection;
}