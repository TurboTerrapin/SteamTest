/*
    PowerControl.cs
    - Handles power-on/power-off procedure
    - Moves power dials, enables power indicators
    Contributor(s): Jake Schott
    Last Updated: 10/21/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PowerControl : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 1.0f;

    private string CONTROL_NAME = "POSITION POWER";
    private static string INFO_MESSAGE = "Controls the enabled status of all controls at the corresponding position (only when ship power is available).";
    private List<string> CONTROL_DESCS = new List<string>{ "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>(){6};
    private List<Button>[] BUTTON_LISTS = new List<Button>[4];

    public PowerManager power_manager;
    public List<GameObject> dials = null;
    public List<GameObject> light_indicator_groups = null;

    private bool[] active_dials = new bool[4] { true, true, true, true };
    private List<string> ray_targets = new List<string>{"pilot_power", "tactician_power", "engineer_power", "captain_power"};
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

        hud_info.setInfo(INFO_MESSAGE);
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

    IEnumerator dialTurn(int index, bool enabling)
    {
        //disable indicator
        if (enabling == false)
        {
            changeIndicator(index, false);
        }
        
        float turn_time = TURN_TIME;
        float starting_angle = dials[index].transform.localEulerAngles.z;
        float dest_angle = 90.0f;
        if (enabling == true)
        {
            dest_angle = 0.0f;
        }

        //turn physical dial
        while (turn_time > 0)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            turn_time = Mathf.Max(0.0f, turn_time - dt);

            float dial_angle = Mathf.Lerp(starting_angle, dest_angle, 1.0f - (turn_time / TURN_TIME));

            dials[index].transform.localRotation =
                Quaternion.Euler(dials[index].transform.localRotation.eulerAngles.x,
                                 dials[index].transform.localRotation.eulerAngles.y,
                                 dial_angle);
            yield return null;
        }

        //enable indicator and station
        if (enabling == true)
        {
            changeIndicator(index, true);
            power_manager.powerStation(index);
        }

        turn_coroutines[index] = null;
    }

    //called by PowerManager
    public void enableDial(int index, bool power_enabled)
    {
        if (power_enabled == true)
        {
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[1]);
        }
        else
        {
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[0]);
        }
        active_dials[index] = true;
        BUTTON_LISTS[index][0].untoggle();
        BUTTON_LISTS[index][0].updateInteractable(true);
    }

    //called by PowerManager
    public void disableDial(int index)
    {
        active_dials[index] = false;
        BUTTON_LISTS[index][0].updateInteractable(false);
    }

    //called by PowerManager
    public void turnDial(int index, bool enabling)
    {
        if (turn_coroutines[index] != null)
        {
            StopCoroutine(turn_coroutines[index]);
        }
        turn_coroutines[index] = StartCoroutine(dialTurn(index, enabling));
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int index = ray_targets.IndexOf(current_target.name);
        if (active_dials[index] == true)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTON_LISTS[index][0].toggle(0.2f);
                transmitPowerControlRPC(index, !power_manager.getPowerEnabled(index));
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitPowerControlRPC(int index, bool enabling)
    {
        if (enabling == true && power_manager.getPowerEnabled(index) == false)
        {
            disableDial(index);
            turnDial(index, true); //will call power_handler.GetComponent<PowerManager>().powerStation(index)
        }
        else if (enabling == false && power_manager.getPowerEnabled(index) == true)
        {
            disableDial(index);
            power_manager.disableStation(index);
        }
    }
}