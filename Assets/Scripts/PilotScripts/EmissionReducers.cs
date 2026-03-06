/*
    EmissionReducers.cs
    - Handles enabling/disabling of port and starboard engine emission reducers
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EmissionReducers : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.5f; //how long the switch takes to be turned
    private static float CHANGE_TIME = 2.0f; //how long it takes for the emission adjustment to take place
    private static float MAX_POWER_CONSUMPTION = 0.4f; //equates to 4 circles (2 per engine)

    private List<string> CONTROL_NAMES = new List<string>() { "PORT EMISSION REDUCER", "STARBOARD EMISSION REDUCER" };
    private static string INFO_MESSAGE = "Enables/disables emission reducers for corresponding engine. Used to conceal ship location and avoid torpedoes.";
    private List<string> CONTROL_DESCS = new List<string>() { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public List<GameObject> emission_switches = null;
    public GameObject emission_display;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private bool[] enabled_reducers = new bool[2] { false, false };
    private float[] enabled_reducer_progress = new float[2] { 0.0f, 0.0f };
    private float[] switch_angles = new float[2] { 90.0f, 90.0f };
    private Coroutine[] reducer_switch_coroutines = new Coroutine[2] { null, null };

    private List<string> ray_targets = new List<string> { "port_reducer_switch", "starboard_reducer_switch" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0], true);
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        hud_info.setButtons(BUTTON_LISTS[0], 6);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 6);
        float power_consumption = 0.0f;
        if (enabled_reducers[index] == true)
        {
            power_consumption = MAX_POWER_CONSUMPTION * 0.5f;
        }
        hud_info.setPowerConsumption(power_consumption);
        return hud_info;
    }

    private void displayReducerChange(int reducer_to_change, float current_percentage)
    {
        Transform current_section = emission_display.transform.GetChild(reducer_to_change); //corresponding engine

        //fill circle
        current_section.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = Mathf.Max(0.05f, current_percentage);

        //emission dots
        for (int i = 0; i <= 2; i++)
        {
            bool is_visible = (current_percentage > 0.33f * (2 - i));
            if (is_visible == true)
            {
                current_section.GetChild(1).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f);
            }
            else
            {
                current_section.GetChild(1).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 0.47f, 0.0f);
            }
        }

        //update arrow
        float a = 1.0f;
        if (current_percentage < 1.0f)
        {
            a = 0.2f;
        }
        current_section.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);

        //update switch material
        if (enabled_reducers[reducer_to_change])
        {
            emission_switches[reducer_to_change].transform.GetChild(0).GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_neon;
        }
        else
        {
            emission_switches[reducer_to_change].transform.GetChild(0).GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_neon;
        }
    }

    //calculates power consumption and updates UI and power management
    private void handlePowerConsumptionChange()
    {
        float consumed_power = 0.0f;
        for (int i = 0; i < 2; i++)
        {
            if (enabled_reducers[i] == true)
            {
                consumed_power += (MAX_POWER_CONSUMPTION * 0.5f);
            }
        }
        ReferenceAssistor.Instance.power_manager.controlPowerChange(0, this.GetType().Name, consumed_power);
    }

    IEnumerator reducerChange(int reducer_to_change, bool to_change_to)
    {
        float anim_time = SWITCH_TIME;
        float starting_rotation = 90.0f;
        float desired_rotation = 180.0f;
        if (to_change_to == false)
        {
            starting_rotation = 180.0f;
            desired_rotation = 90.0f;
            enabled_reducers[reducer_to_change] = false; //disable reducer
            handlePowerConsumptionChange();
            displayReducerChange(reducer_to_change, 1.0f);
        }
        
        //turn the switch
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            switch_angles[reducer_to_change] = Mathf.Lerp(desired_rotation, starting_rotation, anim_time / SWITCH_TIME);
            emission_switches[reducer_to_change].transform.localRotation = Quaternion.Euler(-68.0f, 90.0f, switch_angles[reducer_to_change]);

            yield return null;
        }
        BUTTON_LISTS[reducer_to_change][0].untoggle();

        anim_time = CHANGE_TIME;
        float starting_reducer_percentage = 1.0f;
        float desired_reducer_percentage = 0.0f;
        if (to_change_to == true)
        {
            starting_reducer_percentage = 0.0f;
            desired_reducer_percentage = 1.0f;
        }

        //animate the display
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            float current_reducer_percentage = Mathf.Lerp(desired_reducer_percentage, starting_reducer_percentage, anim_time / CHANGE_TIME);
            enabled_reducer_progress[reducer_to_change] = current_reducer_percentage;
            displayReducerChange(reducer_to_change, current_reducer_percentage);

            yield return null;
        }

        if (to_change_to == false)
        {
            BUTTON_LISTS[reducer_to_change][0].updateDesc(CONTROL_DESCS[0]);
        }
        else
        {
            enabled_reducers[reducer_to_change] = true; //enable reducer
            handlePowerConsumptionChange();
            displayReducerChange(reducer_to_change, 1.0f);
            BUTTON_LISTS[reducer_to_change][0].updateDesc(CONTROL_DESCS[1]);
        }
        BUTTON_LISTS[reducer_to_change][0].updateInteractable(is_powered);

        reducer_switch_coroutines[reducer_to_change] = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int index = ray_targets.IndexOf(current_target.name);

        if (reducer_switch_coroutines[index] == null && is_powered == true)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTON_LISTS[index][0].toggle();
                transmitReducerChangeRPC(index, !enabled_reducers[index]);
            }
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float[] starting_percentages = new float[2] { 0.0f, 0.0f };
        for (int i = 0; i < 2; i++)
        {
            if (reducer_switch_coroutines[i] != null)
            {
                StopCoroutine(reducer_switch_coroutines[i]);
                reducer_switch_coroutines[i] = null;
            }
            BUTTON_LISTS[i][0].updateDesc(CONTROL_DESCS[0]);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][0].untoggle();
            enabled_reducers[i] = false;

            starting_percentages[i] = enabled_reducer_progress[i];
            enabled_reducer_progress[i] = 0.0f;
        }

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            //turn switches
            for (int i = 0; i < 2; i++)
            {
                float percent_enabled = Mathf.Lerp(starting_percentages[i], 0.0f, 1.0f - (anim_time / power_off_time));
                displayReducerChange(i, percent_enabled);
                emission_switches[i].transform.localRotation = Quaternion.Euler(-68.0f, 90.0f, Mathf.Lerp(switch_angles[i], 90.0f, 1.0f - (anim_time / power_off_time)));
            }

            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        emission_display.SetActive(true);
        BUTTON_LISTS[0][0].updateInteractable(true);
        BUTTON_LISTS[1][0].updateInteractable(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        emission_display.SetActive(false);
        hud_info.setPowerConsumption(0.0f);

        //turn off both reducers
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitReducerChangeRPC(int index, bool is_enabled)
    {
        if (reducer_switch_coroutines[index] != null)
        {
            StopCoroutine(reducer_switch_coroutines[index]);
        }
        reducer_switch_coroutines[index] = StartCoroutine(reducerChange(index, is_enabled));
    }
}