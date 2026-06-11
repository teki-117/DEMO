using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNManager
{
    private enum LineKind { Dialogue, Choice, Goto, Game, End }

    private enum AdvanceMode { Manual, Auto, Skip, HoldCtrl }

    private AdvanceMode mode = AdvanceMode.Manual;
    private Coroutine advanceCo;

    private LineKind GetKind(ExcelData d)
    {
        var s = d.speakerName;

        if (s == Constants.CHOICE) return LineKind.Choice;
        if (s == Constants.GOTO) return LineKind.Goto;
        if (s == Constants.GAME) return LineKind.Game;
        if (s == Constants.END_OF_STORY) return LineKind.End;

        return LineKind.Dialogue;
    }
    private void OnDisable()
    {
        mode = AdvanceMode.Manual;
        if (advanceCo != null) StopCoroutine(advanceCo);
        DOTween.Kill(this);
    }
    private void Flow_Next()
    {
        if (typewriterEffect != null && typewriterEffect.IsTyping())
        {
            typewriterEffect.CompleteLine();
            return;
        }

        //if (storyData == null || storyData.Count == 0)
        //{
        //    Debug.LogError(Constants.NO_DATA_FOUND);
        //    ReturnToMenu();
        //    return;
        //}

        //if (currentLine < 0 || currentLine >= storyData.Count)
        //{
        //    ReturnToMenu();
        //    return;
        //}

        if (currentLine > maxReachedLineIndex)
        {
            maxReachedLineIndex = currentLine;
            var dict = GameManager.Instance.maxReachedLineIndices;
            dict[currentStoryFileName] = maxReachedLineIndex;
        }

        var d = storyData[currentLine];
        switch (GetKind(d))
        {
            case LineKind.Choice:
                Choices_Show();
                return;

            case LineKind.Goto:
                Flow_LoadStory(d.speakingContent);
                currentLine = Constants.DEFAULT_START_LINE;
                Flow_Next();
                return;

            //case LineKind.Game:
            //    MiniGame_Load();
            //    return;

            case LineKind.End:
                ReturnToMenu();
                return;

            default:
                Flow_DisplayThisLine();
                return;
        }
    }
    private void Flow_DisplayThisLine()
    {
        var gm = GameManager.Instance;
        gm.currentLineIndex = currentLine;

        var data = storyData[currentLine];

        speakerName.text = LM.GetSpeakerName(data);
        currentSpeakingContent = LM.GetSpeakingContent(data);
        typewriterEffect.StartTyping(currentSpeakingContent, currentTypingSpeed);

        gm.historyRecords.AddLast(data);

        if (!string.IsNullOrEmpty(data.avatarImageFileName))
            Render_UpdateAvatar(data.avatarImageFileName);
        else
            avatarImage.gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(data.vocalAudioFileName))
            Audio_PlayVoice(data.vocalAudioFileName);

        if (!string.IsNullOrEmpty(data.backgroundImageFileName))
        {
            gm.currentBackgroundImg = data.backgroundImageFileName;
            Render_UpdateBackground(data.backgroundImageFileName);
        }

        ApplyCharacterCommands(data.characterCommands);

        currentLine++;
    }
    private void ApplyCharacterCommands(List<CharacterCommand> cmds)
    {
        if (cmds == null) return;

        var gm = GameManager.Instance;

        foreach (var cmd in cmds)
        {
            if (cmd.action == Constants.DISAPPEAR)
            {
                charStates.Remove(cmd.characterID);
                gm.currentCharacterData.RemoveAll(c => c.characterID == cmd.characterID);
                CharacterManager.Instance.HideCharacter(cmd.characterID);
            }
            else
            {
                var state = new CharacterSaveData
                {
                    characterID = cmd.characterID,              
                    positionX = cmd.positionX,
                    scale = cmd.scale,
                    expressionName = cmd.expressionName,
                    motionKey = cmd.motionKey
                };

                charStates[cmd.characterID] = state;
                gm.currentCharacterData.RemoveAll(c => c.characterID == cmd.characterID);
                gm.currentCharacterData.Add(state);
                if (cmd.action.StartsWith(Constants.APPEAR_AT))
                {
                    CharacterManager.Instance.ShowCharacter(
                        cmd.characterID,
                        new Vector2(cmd.positionX, 0f),
                        new Vector2(cmd.scale,cmd.scale),
                        cmd.expressionName,
                        cmd.motionKey
                    );
                }
                else if (cmd.action.StartsWith(Constants.MOVE_TO))
                {
                    CharacterManager.Instance.MoveCharacter(
                        cmd.characterID,
                        cmd.positionX,
                        Constants.DURATION_TIME
                    );
                }
            }
        }
    }
    private void ReturnToMenu()
    {
        GameManager.Instance.hasStarted = false;
        SceneManager.LoadScene(Constants.MENU_SCENE);
    }

    private void SetMode(AdvanceMode m)
    {
        if (mode == m) return;

        mode = m;

        if (advanceCo != null)
            StopCoroutine(advanceCo);

        if (mode != AdvanceMode.Manual)
            advanceCo = StartCoroutine(AutoAdvanceLoop());
        else
            currentTypingSpeed = Constants.DEFAULT_TYPING_SPEED;
    }

    private bool CanSkip() => currentLine < maxReachedLineIndex;

    private IEnumerator AutoAdvanceLoop()
    {
        while (mode != AdvanceMode.Manual)
        {
            if (mode == AdvanceMode.HoldCtrl &&
                !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                SetMode(AdvanceMode.Manual);
                yield break;
            }

            if (mode == AdvanceMode.Skip && !CanSkip())
            {
                SetMode(AdvanceMode.Manual);
                yield break;
            }

            if (!typewriterEffect.IsTyping())
            {
                if (mode == AdvanceMode.Skip || mode == AdvanceMode.HoldCtrl)
                    currentTypingSpeed = Constants.SKIP_MODE_TYPING_SPEED;

                Flow_Next();
            }

            float wait = (mode == AdvanceMode.Auto)
                ? Constants.DEFAULT_AUTO_WAITING_SECONDS
                : Constants.DEFAULT_SKIP_WAITING_SECONDS;

            yield return new WaitForSeconds(wait);
        }
    }
}






        