using UnityEngine;
using UnityEngine.EventSystems;

public partial class VNManager
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (!dialogueBox.activeSelf)
            {
                UI_Open();
            }
            else if (!PointerOverAnyUI() && !ChoiceManager.Instance.choicePanel.activeSelf)
            {
                Flow_Next();
            }
        }
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (dialogueBox.activeSelf)
            {
                UI_Close();
            }
            else
            {
                UI_Open();
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            SetMode(AdvanceMode.HoldCtrl);
        }

    }
    private bool PointerOverAnyUI()
    {
        if (EventSystem.current == null) return false;

        return EventSystem.current.IsPointerOverGameObject();
    }
}

    