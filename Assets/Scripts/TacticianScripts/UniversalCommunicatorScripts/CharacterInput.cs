/*
    CharacterInput.cs
    - Inputs a new numeric/symbol
    Contributor(s): Jake Schott
    Last Updated: 5/16/2025
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterInput : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float PUSH_TIME = 0.25f;

    private string CONTROL_NAME = "CHARACTER INPUT";
    private List<string> CONTROL_DESCS = new List<string> {"INPUT"};
    private List<int> CONTROL_INDEXES = new List<int>() {6};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject input_buttons;

    private Vector3[] initial_pos = new Vector3[12];
    private Vector3 push_direction = new Vector3(0, -0.0034f, 0.0014f);
    private bool is_active = true;
    private Coroutine character_input_coroutine = null;

    private List<string> ray_targets = new List<string> {"A0", "A1", "A2", "A3", "A4", "A5", "B0", "B1", "B2", "B3", "B4", "B5"};

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        hud_info.setButtons(BUTTONS);

        //set initial positions
        for (int i = 0; i < input_buttons.transform.childCount; i++)
        {
            initial_pos[i] = input_buttons.transform.GetChild(i).localPosition;
        }
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    IEnumerator inputCharacter(int button_index)
    {
        Vector3 final_pos = initial_pos[button_index] + push_direction;
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

                input_buttons.transform.GetChild(button_index).transform.localPosition =
                    new Vector3(Mathf.Lerp(initial_pos[button_index].x, final_pos.x, push_percentage),
                                Mathf.Lerp(initial_pos[button_index].y, final_pos.y, push_percentage),
                                Mathf.Lerp(initial_pos[button_index].z, final_pos.z, push_percentage));

                yield return null;
            }

            if (i == 0)
            {
                transform.gameObject.GetComponent<UniversalCommunicator>().updateDisplay();
            }
        }

        if (is_active == true)
        {
            BUTTONS[0].updateInteractable(true);
        }

        character_input_coroutine = null;
    }

    public void activate()
    {
        is_active = true;
        if (character_input_coroutine == null)
        {
            BUTTONS[0].updateInteractable(true);
        }
    }

    public void deactivate()
    {
        is_active = false;
        BUTTONS[0].updateInteractable(false);
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (character_input_coroutine == null && is_active == true && transform.gameObject.GetComponent<UniversalCommunicator>().getIsPowered() == true)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTONS[0].toggle(0.2f);
                transform.gameObject.GetComponent<UniversalCommunicator>().inputCharacter(ray_targets.IndexOf(current_target.name));
            }
        }
    }

    public void pushButton(int index)
    {
        if (character_input_coroutine != null)
        {
            StopCoroutine(character_input_coroutine);
        }
        character_input_coroutine = StartCoroutine(inputCharacter(index));
    }
}
