/*
    PowerRegulationModuleF.cs
    - Handles the turn pattern mini-game in the engineer position
    Contributor(s): Jake Schott
    Last Updated: 10/23/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerRegulationModuleF : NetworkBehaviour, IControllable, IPowerRegulable
{
    //CLASS CONSTANTS
    private static float STATE_CHANGE_TIME = 0.5f;
    private static float CRANK_TIME = 0.5f;

    private string CONTROL_NAME = "ENERGY FIELD EQUALIZER";
    private static string INFO_MESSAGE = "Rotate the crank left or right in the order displayed to complete the module.";
    private List<string> CONTROL_DESCS = new List<string> { "CRANK LEFT", "CRANK RIGHT" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject prsf_display;
    public GameObject prsf_crank;

    private bool currently_active = false;
    private int stage = 0;
    private int turn_configuration = 0; //goes from 0-7 (multiply by 45.0 to get degrees, 0 == 4, 1 == 5, 2 == 6
    private int[] sequence_code = new int[5]{ 0, 0, 0, 0, 0 };
    private Coroutine turn_coroutine = null;
    private Coroutine state_change_coroutine = null;

    private static HUDInfo hud_info = null;

    private void Start()
    {
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));

        hud_info = new HUDInfo(CONTROL_NAME);
        hud_info.setButtons(BUTTONS, 7);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    //sets the state 
    IEnumerator stateChangeHelper(bool to_change_to)
    {
        float anim_time = STATE_CHANGE_TIME;
        float starting_rotation = prsf_crank.transform.localRotation.eulerAngles.z;
        float destination_rotation = 0.0f;

        if (starting_rotation > 180.0f)
        {
            destination_rotation = 360.0f;
        }

        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            //turn the crank
            float turn_rotation = Mathf.Lerp(destination_rotation, starting_rotation, (anim_time / STATE_CHANGE_TIME));
            prsf_crank.transform.localRotation = Quaternion.Euler(-54.0f, -45.0f, turn_rotation);

            yield return null;
        }

        prsf_display.SetActive(to_change_to);

        turn_configuration = 0;
        prsf_crank.transform.localRotation = Quaternion.Euler(-54.0f, -45.0f, 0.0f);
        currently_active = to_change_to;

        state_change_coroutine = null;
    }

    public void resetToDefault()
    {
        if (currently_active == false)
        {
            return;
        }
        currently_active = false;
        prsf_display.SetActive(false);
        for (int i = 0; i < 2; i++)
        {
            BUTTONS[i].updateInteractable(false);
        }
        if (state_change_coroutine != null)
        {
            StopCoroutine(state_change_coroutine);
        }
        state_change_coroutine = StartCoroutine(stateChangeHelper(false));
    }

    public void unlockControl()
    {
        if (currently_active == true)
        {
            return;
        }
        currently_active = true;
        stage = 0;

        if (NetworkManager.Singleton.IsHost == true)
        {
            sequence_code[0] = Random.Range(1, 4);
            for (int i = 1; i < 5; i++)
            {
                int new_turn_direction = Random.Range(0, 2);
                if (new_turn_direction == 0)
                {
                    sequence_code[i] = sequence_code[i - 1] - 1;
                    if (sequence_code[i] < 0)
                    {
                        sequence_code[i] = 7;
                    }
                }
                else
                {
                    sequence_code[i] = sequence_code[i - 1] + 1;
                    if (sequence_code[i] > 7)
                    {
                        sequence_code[i] = 0;
                    }
                }
            }
            generateNewSequenceRPC(sequence_code[0], sequence_code[1], sequence_code[2], sequence_code[3], sequence_code[4]);
        }
    }

    private void displayAdjustment(int to_adjust, bool correct)
    {
        float a = 1.0f;
        if (correct == false)
        {
            a = 0.2f;
        }
        prsf_display.transform.GetChild(to_adjust).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
    }

    IEnumerator crankTurn(int prev_config, int to_turn_to)
    {
        float initial_rotation = prev_config * 45.0f;
        float destination_rotation = to_turn_to * 45.0f;

        if (initial_rotation > 45.0f && destination_rotation == 0.0f)
        {
            destination_rotation = 360.0f;
        }
        else if (initial_rotation == 0.0f && destination_rotation == 315.0f)
        {
            initial_rotation = 360.0f;
        }

        float anim_time = CRANK_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            prsf_crank.transform.localRotation = 
                Quaternion.Euler(-54.0f, -45.0f, Mathf.Lerp(initial_rotation, destination_rotation, 1.0f - (anim_time / CRANK_TIME)));

            yield return null;
        }

        BUTTONS[0].untoggle();
        BUTTONS[1].untoggle();

        if ((to_turn_to % 4) == (sequence_code[stage] % 4))
        {
            displayAdjustment(stage, true);

            if (stage == 4)
            {
                if (NetworkManager.Singleton.IsHost == true)
                {
                    transmitModuleCompletionRPC();
                }
            }
            else
            {
                stage += 1;
                BUTTONS[0].updateInteractable(true);
                BUTTONS[1].updateInteractable(true);
            }
        }
        else
        {
            stage = 0;
            for (int i = 0; i < 5; i++)
            {
                displayAdjustment(i, false);
            }
            if ((to_turn_to % 4) == (sequence_code[0] % 4))
            {
                displayAdjustment(0, true);
                stage = 1;
            }
            BUTTONS[0].updateInteractable(true);
            BUTTONS[1].updateInteractable(true);
        }

        turn_configuration = to_turn_to;

        turn_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (currently_active == false)
        {
            return;
        }

        if (turn_coroutine == null && BUTTONS[0].getInteractable() == true)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //crank left
            {
                BUTTONS[0].toggle();
                BUTTONS[1].updateInteractable(false);
                int new_turn_config = turn_configuration - 1;
                if (new_turn_config < 0)
                {
                    new_turn_config = 7;
                }
                crankTurnRPC(new_turn_config, stage);
            }
            else if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //crank right
            {
                BUTTONS[1].toggle();
                BUTTONS[0].updateInteractable(false);
                int new_turn_config = turn_configuration + 1;
                if (new_turn_config > 7)
                {
                    new_turn_config = 0;
                }
                crankTurnRPC(new_turn_config, stage);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void generateNewSequenceRPC(int turn_a, int turn_b, int turn_c, int turn_d, int turn_e)
    {
        sequence_code[0] = turn_a;
        sequence_code[1] = turn_b;
        sequence_code[2] = turn_c;
        sequence_code[3] = turn_d;
        sequence_code[4] = turn_e;
        stage = 0;
        for (int i = 0; i < 5; i++)
        {
            prsf_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
            prsf_display.transform.GetChild(i).localRotation = Quaternion.Euler(0.0f, 0.0f, sequence_code[i] * 45.0f);
        }
        prsf_display.SetActive(true);
        for (int i = 0; i < 2; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void crankTurnRPC(int turn_config, int new_stage)
    {
        stage = new_stage;
        if (turn_coroutine != null)
        {
            StopCoroutine(turn_coroutine);
        }

        turn_coroutine = StartCoroutine(crankTurn(turn_configuration, turn_config));
    }

    //called by host when mini-game completed
    [Rpc(SendTo.Everyone)]
    private void transmitModuleCompletionRPC()
    {
        GameObject.Find("PowerHandler").GetComponent<PowerRegulator>().moduleCompleted(this.GetType().Name);
    }
}