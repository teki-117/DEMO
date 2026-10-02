using UnityEngine;

public static class CharacterDisplayUtility
{
    public static string GetName(CharacterData data)
    {
        if (data == null)
            return "";

        PartyManager party = PartyManager.Instance;
        GameManager game = GameManager.Instance;

        if (party != null &&
            party.IsMainCharacter(data.characterID) &&
            game != null &&
            !string.IsNullOrWhiteSpace(game.playerName))
        {
            return game.playerName;
        }

        return string.IsNullOrWhiteSpace(data.displayName)
            ? data.characterID
            : data.displayName;
    }
}