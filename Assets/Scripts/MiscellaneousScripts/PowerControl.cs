/*
    PowerControl.cs
    - Handles power-on/power-off procedure
    - Moves power dials, enables power indicators
    Contributor(s): Jake Schott
    Last Updated: 5/14/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PowerControl : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 1.0f;
    private static float COOLDOWN_TIME = 0.25f;

    private string CONTROL_NAME = "POWER CONTROL";
    private List<string> CONTROL_DESCS = new List<string>{"ENABLE"};
    private List<int> CONTROL_INDEXES = new List<int>(){6};
    private List<Button>[] BUTTON_LISTS = new List<Button>[4];

    public List<GameObject> dials = null;
    public List<GameObject> light_indicator_groups = null;

    private List<string> ray_targets = new List<string>{"pilot_power", "tactician_power", "engineer_power", "captain_power"};
    private bool[] power_enabled = new bool[4] { false, false, false, false };
    private Coroutine[] turn_coroutines = new Coroutine[4] {null, null, null, null};

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        for (int i = 0; i < 4; i++)
        {
            BUTTON_LISTS[i] = new List<Button>();
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, true));
        }
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setButtons(BUTTON_LISTS[index]);
        return hud_info;
    }
    
    //updates knob light, adjacent circle lights (for all positions)
    private void changeIndicator(int index, bool active)
    {
        dials[index].transform.GetChild(1).GetChild(0).GetChild(1).gameObject.SetActive(active);
        for (int i = 0; i < light_indicator_groups.Count; i++)
        {
            light_indicator_groups[i].transform.GetChild(index).GetChild(0).GetChild(1).gameObject.SetActive(active);
        }
    }

    IEnumerator dialTurn(int index)
    {
        bool increasing = true;

        //disable indicator
        if (power_enabled[index] == true)
        {
            changeIndicator(index, false);
            power_enabled[index] = false;
            increasing = false;
        }
        
        float turn_time = TURN_TIME;

        //turn physical dial
        while (turn_time > 0)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            turn_time = Mathf.Max(0.0f, turn_time - dt);

            float dial_angle = Mathf.Lerp(-90, 0, turn_time / TURN_TIME);
            if (increasing == true)
            {
                dial_angle = Mathf.Lerp(-90, 0, 1.0f - (turn_time / TURN_TIME));
            }

            dials[index].transform.localRotation =
                Quaternion.Euler(dials[index].transform.localRotation.eulerAngles.x,
                                 dials[index].transform.localRotation.eulerAngles.y,
                                 dial_angle);
            yield return null;
        }

        //enable indicator
        if (increasing == true)
        {
            changeIndicator(index, true);
            power_enabled[index] = true;
        }

        //cooldown
        yield return new WaitForSeconds(COOLDOWN_TIME);

        BUTTON_LISTS[index][0].updateInteractable(true);
        if (power_enabled[index])
        {
            BUTTON_LISTS[index][0].updateDesc("DISABLE");
        }
        else
        {
            BUTTON_LISTS[index][0].updateDesc("ENABLE");
        }

        turn_coroutines[index] = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int index = ray_targets.IndexOf(current_target.name);
        if (turn_coroutines[index] == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTON_LISTS[index][0].toggle(0.2f);
                transmitPowerControlRPC(index, power_enabled[index]);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitPowerControlRPC(int index, bool is_enabled)
    {
        power_enabled[index] = is_enabled;
        if (turn_coroutines[index] != null)
        {
            StopCoroutine(turn_coroutines[index]);
        }
        turn_coroutines[index] = StartCoroutine(dialTurn(index));
    }
}