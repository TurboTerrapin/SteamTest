/*
    CharacterInput.cs
    - Inputs a new numeric/symbol
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static AnimatorHandler;

public class CharacterInput : NetworkBehaviour, IControllable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float PUSH_TIME = 0.25f;
    private static Vector3 FINAL_POS = new Vector3(0.0f, -0.004f, 0.0016f);

    private string CONTROL_NAME = "CHARACTER INPUT";
    private static string INFO_MESSAGE = "Input characters or symbols into the universal communicator.";
    private List<string> CONTROL_DESCS = new List<string> { "INPUT" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject input_buttons;

    private UniversalCommunicator universal_communicator;

    private bool is_active = false;
    private Vector3[] initial_pos = new Vector3[12];
    private Coroutine character_input_coroutine = null;

    private List<string> ray_targets = new List<string> {"A0", "A1", "A2", "A3", "A4", "A5", "B0", "B1", "B2", "B3", "B4", "B5"};

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public int finger_position = 0;

    private void Start()
    {
        universal_communicator = GetComponent<UniversalCommunicator>();

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

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
    public Transform getIKTarget(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        return IK_targets[index].transform;
    }
    public AnimatorHandler.HandInteractionType getHandInteractionType()
    {
        return hand_interaction_type;
    }
    public float getHandPose()
    {
        return hand_pose;
    }
    public bool getRightHandFlip()
    {
        return does_right_hand_flip;
    }

    IEnumerator inputCharacter(int button_index)
    {
        Vector3 final_pos = initial_pos[button_index] + FINAL_POS;
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

                input_buttons.transform.GetChild(button_index).transform.localPosition = Vector3.Lerp(initial_pos[button_index], final_pos, push_percentage);

                yield return null;
            }

            if (i == 0)
            {
                universal_communicator.onInputChange();
            }
        }

        BUTTONS[0].updateInteractable(is_active);

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
        if (character_input_coroutine != null || is_active == false)
        {
            return;
        }

        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
        {
            BUTTONS[0].toggle(0.1f);
            universal_communicator.inputCharacter(ray_targets.IndexOf(current_target.name));
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