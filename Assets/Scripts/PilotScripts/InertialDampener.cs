/*
    InertialDampener.cs
    - Handles inertial dampener
    - When enabled, increase acceleration rates for thrusters and impulse throttle
    Contributor(s): Jake Schott
    Last Updated: 3/10/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InertialDampener : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.5f;
    private static float MAX_POWER_CONSUMPTION = 0.3f; //equates to 3 circles

    private string CONTROL_NAME = "INERTIAL DAMPENER";
    private static string INFO_MESSAGE = "Increases ship acceleration for thrusters and impulse throttle.";
    private List<string> CONTROL_DESCS = new List<string>() { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>(); 

    public GameObject dampener_switch;
    public GameObject dampener_display;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private bool dampener_is_enabled = false;
    private float dampener_enabled_percentage = 0.0f;
    private Coroutine dampener_switch_coroutine = null;

    private static HUDInfo hud_info = null;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME, true);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    private float getInertialDampenerModifierValue()
    {
        if (dampener_is_enabled == true)
        {
            return 1.0f;
        }
        return 0.0f;
    }

    private void adjustInertialDampenerModifiers()
    {
        float modifier = getInertialDampenerModifierValue();
        GetComponent<ImpulseThrottle>().adjustInertialDampenerModifier(modifier);
        GetComponent<HorizontalThrusters>().adjustInertialDampenerModifier(modifier);
        GetComponent<VerticalThrusters>().adjustInertialDampenerModifier(modifier);
    }

    IEnumerator switchDampener(bool to_switch_to)
    {
        float starting_switch_rotation = dampener_switch.transform.localRotation.eulerAngles.z;
        float desired_switch_rotation = 180.0f;

        dampener_is_enabled = to_switch_to;


        if (to_switch_to == true)
        {
            desired_switch_rotation = 90.0f;
            ReferenceAssistor.Instance.power_manager.controlPowerChange(0, this.GetType().Name, MAX_POWER_CONSUMPTION);
            hud_info.setPowerConsumption(MAX_POWER_CONSUMPTION);
        }
        else
        {
            ReferenceAssistor.Instance.power_manager.controlPowerChange(0, this.GetType().Name, 0.0f);
            hud_info.setPowerConsumption(0.0f);
        }

        float anim_time = SWITCH_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            dampener_enabled_percentage = anim_time / SWITCH_TIME;
            if (to_switch_to == true)
            {
                dampener_enabled_percentage = 1.0f - dampener_enabled_percentage;
            }

            //turn switch
            dampener_switch.transform.localRotation =
                Quaternion.Euler(-113.0f, 0.0f, Mathf.Lerp(starting_switch_rotation, desired_switch_rotation, 1.0f - (anim_time / SWITCH_TIME)));

            //adjust fill bar
            dampener_display.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = dampener_enabled_percentage;

            yield return null;
        }

        adjustInertialDampenerModifiers();

        if (to_switch_to == true)
        {
            BUTTONS[0].updateDesc(CONTROL_DESCS[1]);
        }
        else
        {
            BUTTONS[0].updateDesc(CONTROL_DESCS[0]);
        }

        BUTTONS[0].updateInteractable(is_powered);

        dampener_switch_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (dampener_switch_coroutine == null && is_powered == true)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTONS[0].toggle(0.2f);
                BUTTONS[0].updateInteractable(false);
                transmitInertialDampenerRPC(dampener_is_enabled);
            }
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float starting_rotation = Mathf.Lerp(90.0f, 180.0f, dampener_enabled_percentage);

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            dampener_switch.transform.localRotation =
                Quaternion.Euler(-113.0f, 0.0f, Mathf.Lerp(starting_rotation, 180.0f, 1.0f - (anim_time / power_off_time)));

            yield return null;
        }

        adjustInertialDampenerModifiers();

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        BUTTONS[0].updateInteractable(true);
        dampener_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        dampener_display.SetActive(false);
        hud_info.setPowerConsumption(0.0f);

        if (dampener_switch_coroutine != null)
        {
            StopCoroutine(dampener_switch_coroutine);
            dampener_switch_coroutine = null;
        }
        dampener_display.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = 0.0f;
        dampener_is_enabled = false;
        dampener_enabled_percentage = 0.0f;
        BUTTONS[0].updateDesc(CONTROL_DESCS[0]);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[0].untoggle();

        //turn off all inertial dampeners
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitInertialDampenerRPC(bool is_enabled)
    {
        dampener_is_enabled = is_enabled;
        if (dampener_switch_coroutine != null)
        {
            StopCoroutine(dampener_switch_coroutine);
        }
        dampener_switch_coroutine = StartCoroutine(switchDampener(!is_enabled));
    }
}