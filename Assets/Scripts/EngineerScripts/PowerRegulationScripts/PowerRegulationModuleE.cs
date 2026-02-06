/*
    PowerRegulationModuleE.cs
    - Handles the memorization switch minigame in the engineer position
    Contributor(s): Jake Schott
    Last Updated: 10/23/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerRegulationModuleE : NetworkBehaviour, IControllable, IPowerRegulable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.25f;

    private string[] CONTROL_NAMES = new string[5] { "SECURITY CODE OPTION A", "SECURITY CODE OPTION B", "SECURITY CODE OPTION C", "SECURITY CODE OPTION D", "SECURITY CODE OPTION E" };
    private static string INFO_MESSAGE = "Analyze the code and enter the five corresponding color symbols (in order) to complete the module.";
    private List<string> CONTROL_DESCS = new List<string> { "GREEN", "BLUE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 2, 0 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[5] { new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>() };

    private List<string> ray_targets = new List<string> { "prse_switch_a", "prse_switch_b", "prse_switch_c", "prse_switch_d", "prse_switch_e" };

    public GameObject prse_display;
    public List<GameObject> prse_code_displays = null;
    public List<GameObject> prse_switches = null;

    private UnityEngine.UI.RawImage symbol_icon;
    private GameObject code_progress;

    private bool currently_active = false;
    private int stage = 0;
    private int[] correct_code = new int[5]{ 0, 0, 0, 0, 0 };
    private Coroutine code_sequence_coroutine = null;
    private Coroutine code_input_coroutine = null;

    private static HUDInfo hud_info = null;

    private void Start()
    {
        symbol_icon = prse_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>();
        code_progress = prse_display.transform.GetChild(1).gameObject;

        for (int i = 0; i < 5; i++)
        {
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        }

        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        hud_info.setButtons(BUTTON_LISTS[0], 7);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 7);

        return hud_info;
    }

    public void resetToDefault()
    {
        if (currently_active == false)
        {
            return;
        }
        currently_active = false;
        prse_display.SetActive(false);
        for (int i = 0; i < 2; i++)
        {
            prse_code_displays[i].SetActive(false);
        }
        for (int i = 0; i < 5; i++)
        {
            BUTTON_LISTS[i][0].untoggle();
            BUTTON_LISTS[i][1].untoggle();
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
        }
    }

    public void unlockControl()
    {
        if (currently_active == true)
        {
            return;
        }
        currently_active = true;

        if (NetworkManager.Singleton.IsHost == true)
        {
            int first_symbol = Random.Range(0, 10);
            correct_code[0] = first_symbol;
            for (int i = 1; i < 5; i++)
            {
                List<int> possible_options = new List<int>();
                for (int x = 0; x < 10; x++)
                {
                    if (correct_code[i - 1] != x && ((correct_code[i - 1] % 5) != (x % 5)))
                    {
                        possible_options.Add(x);
                    }
                }
                correct_code[i] = possible_options[Random.Range(0, possible_options.Count)];
            }
            transmitNewCodeSequenceRPC(correct_code[0], correct_code[1], correct_code[2], correct_code[3], correct_code[4]);
        }
    }

    private void displayAdjustment()
    {
        for (int i = 0; i < 5; i++)
        {
            if (i < stage)
            {
                code_progress.transform.GetChild(i).gameObject.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
            }
            else
            {
                code_progress.transform.GetChild(i).gameObject.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
            }
            code_progress.transform.GetChild(i).GetChild(0).gameObject.SetActive(i >= stage);
        }
    }

    IEnumerator cyclingCodeDisplay()
    {
        while (true)
        {
            for (int i = 0; i < 5; i++)
            {
                UnityEngine.UI.RawImage matching_symbol = null;
                if (correct_code[i] > 4) //green
                {
                    matching_symbol = prse_code_displays[1].transform.GetChild(correct_code[i] - 5).GetComponent<UnityEngine.UI.RawImage>();
                }
                else
                {
                    matching_symbol = prse_code_displays[0].transform.GetChild(correct_code[i]).GetComponent<UnityEngine.UI.RawImage>();
                }
                symbol_icon.texture = matching_symbol.texture;
                symbol_icon.color = matching_symbol.color;
                symbol_icon.gameObject.SetActive(true);
                yield return new WaitForSeconds(1.25f);
            }
            symbol_icon.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator codeInput(int to_switch)
    {
        for (int i = 0; i < 5; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
        }

        float initial_rotation = -55.0f;
        float destination_rotation = -80.0f;

        int switch_index = to_switch;
        if (to_switch > 4) //green
        {
            destination_rotation = -30.0f;
            switch_index -= 5;
        }

        float anim_time = SWITCH_TIME;
        for (int i = 0; i <= 1; i++)
        {
            float half_time = SWITCH_TIME * 0.5f;
            float curr_time = half_time;

            while (curr_time > 0.0f)
            {
                curr_time = Mathf.Max(0.0f, curr_time - Time.deltaTime);

                float switch_percentage = 1.0f - (curr_time / half_time);
                if (i == 1)
                {
                    switch_percentage = (curr_time / half_time);
                }

                prse_switches[switch_index].transform.localRotation = Quaternion.Euler(Mathf.Lerp(initial_rotation, destination_rotation, switch_percentage), 315.0f, 0.0f);

                yield return null;
            }

            if (i == 0)
            {
                if (currently_active == true)
                {
                    if (correct_code[stage] == to_switch) //right code
                    {
                        if (stage >= 4) //last digit
                        {
                            if (NetworkManager.Singleton.IsHost == true)
                            {
                                transmitModuleCompletionRPC();
                            }
                        }
                        else //not last digit
                        {
                            stage += 1;
                            if (NetworkManager.Singleton.IsHost == true)
                            {
                                transmitStageChangeRPC(stage);
                            }
                        }
                    }
                    else //wrong code
                    {
                        stage = 0;
                        if (NetworkManager.Singleton.IsHost == true)
                        {
                            transmitStageChangeRPC(stage);
                        }
                    }
                }
            }
        }

        for (int i = 0; i < 5; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(stage < 5 && currently_active == true);
            BUTTON_LISTS[i][1].updateInteractable(stage < 5 && currently_active == true);
        }

        code_input_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (currently_active == false)
        {
            return;
        }

        int target_index = ray_targets.IndexOf(current_target.name);

        if (code_input_coroutine == null && BUTTON_LISTS[0][0].getInteractable() == true)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //green
            {
                BUTTON_LISTS[target_index][0].toggle();
                BUTTON_LISTS[target_index][1].updateInteractable(false);
                codeInputRPC(target_index + 5, stage);
            }
            else if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //blue
            {
                BUTTON_LISTS[target_index][1].toggle();
                BUTTON_LISTS[target_index][0].updateInteractable(false);
                codeInputRPC(target_index, stage);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitStageChangeRPC(int new_stage) 
    {
        stage = new_stage;
        displayAdjustment();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitNewCodeSequenceRPC(int code_a, int code_b, int code_c, int code_d, int code_e)
    {
        correct_code[0] = code_a;
        correct_code[1] = code_b;
        correct_code[2] = code_c;
        correct_code[3] = code_d;
        correct_code[4] = code_e;
        stage = 0;
        displayAdjustment();
        
        for (int i = 0; i < 2; i++)
        {
            prse_code_displays[i].SetActive(true);
        }

        prse_display.SetActive(true);

        for (int i = 0; i < 5; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(true);
            BUTTON_LISTS[i][1].updateInteractable(true);
        }

        if (code_sequence_coroutine != null)
        {
            StopCoroutine(code_sequence_coroutine);
        }
        code_sequence_coroutine = StartCoroutine(cyclingCodeDisplay());
    }

    [Rpc(SendTo.Everyone)]
    private void codeInputRPC(int switch_config, int new_stage)
    {
        stage = new_stage;
        if (code_input_coroutine != null)
        {
            StopCoroutine(code_input_coroutine);
        }

        code_input_coroutine = StartCoroutine(codeInput(switch_config));
    }

    //called by host when mini-game completed
    [Rpc(SendTo.Everyone)]
    private void transmitModuleCompletionRPC()
    {
        GameObject.Find("PowerHandler").GetComponent<PowerRegulator>().moduleCompleted(this.GetType().Name);
    }
}