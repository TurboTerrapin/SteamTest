/*
    PowerControl.cs
    - Handles power-on/power-off procedure
    - Handles orange flashing in all four positions if a player is seated, power is available, but they are not activating the power dial
    - Moves power dials, enables power indicators
    Contributor(s): Jake Schott
    Last Updated: 12/9/2025
*/

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerControl : NetworkBehaviour, IControllable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 1.0f;
    private static float PLAYER_NOTIFIER_REFRESH = 0.5f;

    private string CONTROL_NAME = "POSITION POWER";
    private static string INFO_MESSAGE = "Controls the enabled status of all controls at the corresponding position (only when ship power is available).";
    private List<string> CONTROL_DESCS = new List<string>{ "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>(){6};
    private List<Button>[] BUTTON_LISTS = new List<Button>[4];

    public PowerManager power_manager;
    public List<GameObject> dials = null;
    public GameObject dial_sounds;
    public List<GameObject> light_indicator_groups = null;
    public List<GameObject> IK_targets = null;

    private bool[] active_dials = new bool[4] { true, true, true, true };
    private bool[] current_seats = new bool[4] { false, false, false, false };
    private List<string> ray_targets = new List<string>{"pilot_power", "tactician_power", "engineer_power", "captain_power"};
    private Coroutine[] turn_coroutines = new Coroutine[4] {null, null, null, null};
    private Coroutine player_notifier_coroutine = null;

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

    public Transform getIKTarget()
    {
        return IK_targets[0].transform;
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
        dials[index].transform.GetChild(1).GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f);
        for (int i = 0; i < light_indicator_groups.Count; i++)
        {
            light_indicator_groups[i].transform.GetChild(index).GetChild(0).GetChild(1).gameObject.SetActive(active);
            light_indicator_groups[i].transform.GetChild(index).GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f);
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
            power_manager.powerStation(index);
            updatePlayerNotifiers(index, current_seats);
            changeIndicator(index, true);
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

    //handles the coroutine that does the orange flashing
    public void updatePlayerNotifiers()
    {
        for (int i = 0; i < 4; i++)
        {
            updatePlayerNotifiers(i, current_seats);
        }
    }

    //handles the coroutine that does the orange flashing 
    public void updatePlayerNotifiers(int updated_position, bool[] occupied_seats)
    {
        //update seats
        current_seats = occupied_seats;

        //handle starting/stopping beep sound
        if (power_manager.getPowerEnabled(updated_position) == true || occupied_seats[updated_position] == false || power_manager.getShipHasPower() == false)
        {
            dial_sounds.transform.GetChild(updated_position).GetComponent<AudioSource>().Stop();
        }
        else if (occupied_seats[updated_position] == true && power_manager.getShipHasPower() == true)
        {
            if (dial_sounds.transform.GetChild(updated_position).GetComponent<AudioSource>().isPlaying == false)
            {
                dial_sounds.transform.GetChild(updated_position).GetComponent<AudioSource>().Play();
            }
        }

        //check if need to stop the player_notifier_coroutine
        if (player_notifier_coroutine != null)
        {
            bool keep_going = false;

            //check if needing to end
            for (int i = 0; i < 4; i++)
            {
                if (power_manager.getPowerEnabled(i) == false && occupied_seats[i] == true)
                {
                    keep_going = true;
                }
            }

            if (power_manager.getShipHasPower() == false)
            {
                keep_going = false;
            }

            //end the coroutine
            if (keep_going == false)
            {
                StopCoroutine(player_notifier_coroutine);
                player_notifier_coroutine = null;

                for (int i = 0; i < 4; i++)
                {
                    changeIndicator(i, power_manager.getPowerEnabled(i));
                }
            }

            return;
        }

        //check if need to start the player_notifier_coroutine
        if (power_manager.getShipHasPower() == true)
        {
            for (int i = 0; i < 4; i++)
            {
                if (power_manager.getPowerEnabled(i) == false && occupied_seats[i] == true)
                {
                    player_notifier_coroutine = StartCoroutine(playerNotifier());
                    return;
                }
            }
        }
    }

    //helper method used to update the orange flashing lights
    private void updateNotifierIndicator(int index, Color orange_color)
    {
        //do nothing if power is enabled at that position
        if (power_manager.getPowerEnabled(index) == true)
        {
            return;
        }

        //show/hide the indicator
        for (int i = 0; i < 4; i++)
        {
            dials[index].transform.GetChild(1).GetChild(0).GetChild(1).gameObject.SetActive(current_seats[index]);
            light_indicator_groups[i].transform.GetChild(index).GetChild(0).GetChild(1).gameObject.SetActive(current_seats[index]);
        }

        //update the color (alpha)
        if (current_seats[index] == true)
        {
            for (int i = 0; i < 4; i++)
            {
                dials[index].transform.GetChild(1).GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = orange_color;
                light_indicator_groups[i].transform.GetChild(index).GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = orange_color;
            }
        }
    }

    //infinite loop that handles flashing the orange indicators when a player is sitting at a position with the power on but not turning the dial (works for all four positions)
    IEnumerator playerNotifier()
    {
        float anim_time = PLAYER_NOTIFIER_REFRESH;
        Color orange_color;
        while (true)
        {
            for (int x = 0; x < 2; x++)
            {
                anim_time = PLAYER_NOTIFIER_REFRESH;
                while (anim_time > 0.0f)
                {
                    anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

                    if (x == 0)
                    {
                        orange_color = new Color(1.0f, 0.47f, 0.0f, 1.0f - (0.8f * (1.0f - (anim_time / PLAYER_NOTIFIER_REFRESH))));
                    }
                    else
                    {
                        orange_color = new Color(1.0f, 0.47f, 0.0f, 0.2f + (0.8f * (1.0f - (anim_time / PLAYER_NOTIFIER_REFRESH))));
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        updateNotifierIndicator(i, orange_color);
                    }

                    yield return null;
                }
            }
        }
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
            turnDial(index, true); //will call power_manager.powerStation(index)
        }
        else if (enabling == false && power_manager.getPowerEnabled(index) == true)
        {
            disableDial(index);
            power_manager.disableStation(index);
        }
        updatePlayerNotifiers(index, current_seats);
    }
}