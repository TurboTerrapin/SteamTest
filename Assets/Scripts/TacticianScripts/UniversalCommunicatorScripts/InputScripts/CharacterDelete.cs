/*
    CharacterDelete.cs
    - Acts as a backspace for the UniversalCommunicator
    Contributor(s): Jake Schott
    Last Updated: 1/2/2026
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterDelete : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float PUSH_TIME = 0.25f;
    private static Vector3 FINAL_POS = new Vector3(0.0f, -0.0055f, 0.0022f);

    private string CONTROL_NAME = "DELETE CHARACTER";
    private static string INFO_MESSAGE = "Deletes the last character in the code display in input mode.";
    private List<string> CONTROL_DESCS = new List<string> { "DELETE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject delete_button;

    private UniversalCommunicator universal_communicator;

    bool is_active = false;
    private Vector3 initial_pos;
    private Coroutine character_delete_coroutine = null;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        universal_communicator = transform.GetComponent<UniversalCommunicator>();

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

        //set initial position
        initial_pos = delete_button.transform.localPosition;
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public void activate()
    {
        is_active = true;
        BUTTONS[0].updateInteractable(true);
    }

    public void deactivate()
    {
        is_active = false;
        BUTTONS[0].updateInteractable(false);
    }

    IEnumerator characterDeletion()
    {
        for (int i = 0; i <= 1; i++)
        {
            float half_time = PUSH_TIME * 0.5f;
            float push_time = half_time;

            while (push_time > 0.0f)
            {
                float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
                push_time = Mathf.Max(0.0f, push_time - dt);

                float push_percentage = 1.0f - (push_time / half_time);
                if (i == 1)
                {
                    push_percentage = (push_time / half_time);
                }

                delete_button.transform.localPosition = Vector3.Lerp(initial_pos, FINAL_POS, push_percentage);

                yield return null;
            }

            if (i == 0)
            {
                universal_communicator.onInputChange();
            }
        }

        BUTTONS[0].updateInteractable(is_active);

        character_delete_coroutine = null;
    }

    public void pushDeleteButton()
    {
        if (character_delete_coroutine != null)
        {
            StopCoroutine(character_delete_coroutine);
        }
        StartCoroutine(characterDeletion());
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (character_delete_coroutine == null && is_active == true)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTONS[0].toggle(0.1f);
                universal_communicator.deleteLastCharacter();
            }
        }
    }
}
