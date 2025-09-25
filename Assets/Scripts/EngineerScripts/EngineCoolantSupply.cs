/*
    EngineCoolantSupply.cs
    - Handles turning of wheel to increase coolant and reduce engine temperature
    - Increases engine temperature over time
    - Tells PilotingSystem to reduce speed when engines are overheated
    Contributor(s): Jake Schott
    Last Updated: 9/23/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EngineCoolantSupply : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float TURN_SPEED = 0.1f;
    private static float IMPULSE_SPEED_CHANGE_FACTOR = 4.0f; //goes 1/4 as fast when engines are overheated
    private static float ENGINE_TEMPERATURE_INCREASE_SPEED = 0.025f;
    private static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles

    private string CONTROL_NAME = "ENGINE COOLANT SUPPLY";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public Material lit_neon;
    public Material unlit_neon;

    public GameObject engine_coolant_supply_display;
    public GameObject coolant_wheel;

    private PilotingSystem piloting_system;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;

    private float coolant_flow = 0.0f;
    private float engine_temperature = 0.05f;
    private float impulse_speed_modifier = 1.0f;
    private Coroutine engine_temperature_increase_coroutine = null;
    private Coroutine engine_speed_change_coroutine = null;

    private static HUDInfo hud_info = null;

    private void Start()
    {
        piloting_system = GameObject.FindGameObjectWithTag("Spaceship").GetComponent<PilotingSystem>();

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false)); //increase button
        hud_info.setButtons(BUTTONS, 7);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    private void displayCoolantFlowAdjustment()
    {
        //update wheel rotation
        coolant_wheel.transform.localRotation = Quaternion.Euler(-54.0f, 315.0f, coolant_flow * 1080.0f);

        //update screen wheel
        engine_coolant_supply_display.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = coolant_flow;
        engine_coolant_supply_display.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().color = new Color(0.0f, 0.84f, 1.0f, 0.05f + (0.95f * coolant_flow));
    }

    private void displayEngineTemperatureAdjustment()
    {
        Color status_color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        if (engine_temperature >= 1.0f)
        {
            status_color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
        else if (engine_temperature > 0.5)
        {
            status_color = new Color(0.84f, 0.62f, 0.0f, 1.0f);
        }

        //update screen (left bar and right bar are the same)
        for (int i = 0; i < 2; i++)
        {
            //update colors
            engine_coolant_supply_display.transform.GetChild(i + 3).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
            engine_coolant_supply_display.transform.GetChild(i + 3).transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().color = status_color;

            //update fill bar
            engine_coolant_supply_display.transform.GetChild(i + 3).transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = Mathf.Max(0.05f, engine_temperature);
        }

        //inner and outer rings
        engine_coolant_supply_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        engine_coolant_supply_display.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        engine_coolant_supply_display.transform.GetChild(2).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        engine_coolant_supply_display.transform.GetChild(2).GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
    }

    IEnumerator engineTemperatureIncreaser()
    {
        while (engine_temperature < 1.0f)
        {
            float difference = ENGINE_TEMPERATURE_INCREASE_SPEED - (coolant_flow * (ENGINE_TEMPERATURE_INCREASE_SPEED * 2.5f));
            if (difference > 0.0f)
            {
                engine_temperature = Mathf.Min(1.0f, engine_temperature + (difference * Time.deltaTime));
            }
            else
            {
                engine_temperature = Mathf.Max(0.0f, engine_temperature + (difference * Time.deltaTime));
            }
            if (is_powered == true)
            {
                transmitEngineTemperatureChangeRPC(engine_temperature);
            }
            yield return null;
        }

        transmitEngineOverheatRPC();

        engine_temperature_increase_coroutine = null;
    }

    IEnumerator engineSpeedChange(float to_change_to)
    {
        float anim_time = 1.0f; //takes one second to update
        float starting_impulse_speed_modifier = impulse_speed_modifier;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            impulse_speed_modifier = Mathf.Lerp(starting_impulse_speed_modifier, to_change_to, 1.0f - anim_time);
            piloting_system.AdjustMaxImpulseSpeed(impulse_speed_modifier);

            yield return null;
        }

        engine_speed_change_coroutine = null;
    }

    public void initializeEngineTemperatureIncreaser()
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (engine_temperature_increase_coroutine != null)
            {
                StopCoroutine(engine_temperature_increase_coroutine);
            }

            engine_temperature = 0.0f;
            engine_temperature_increase_coroutine = StartCoroutine(engineTemperatureIncreaser());
        }
    }

    public void resetEngineTemperatureIncreaser()
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (engine_temperature_increase_coroutine != null)
            {
                StopCoroutine(engine_temperature_increase_coroutine);
                engine_temperature_increase_coroutine = null;
            }

            if (engine_speed_change_coroutine != null)
            {
                StopCoroutine (engine_speed_change_coroutine);
                engine_speed_change_coroutine = null;
            }

            piloting_system.AdjustMaxImpulseSpeed(1.0f);
            transmitEngineTemperatureChangeRPC(0.0f);
        }
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int turn_direction = 0;
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to to increase
        {
            turn_direction += 1;
        }
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))  //Q to decrease
        {
            turn_direction -= 1;
        }
        if (turn_direction != 0)
        {
            if (turn_direction > 0)
            {
                coolant_flow = Mathf.Min(1.0f, coolant_flow + (dt * TURN_SPEED));
            }
            else
            {
                coolant_flow = Mathf.Max(0.0f, coolant_flow - (dt * TURN_SPEED));
            }
            BUTTONS[0].updateInteractable(coolant_flow > 0.0f);
            BUTTONS[1].updateInteractable(coolant_flow < 1.0f);
            transmitCoolantFlowAdjustmentRPC(coolant_flow);
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float start_cf = coolant_flow;
        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            coolant_flow = Mathf.Lerp(start_cf, 0.0f, 1.0f - (anim_time / power_off_time));
            displayCoolantFlowAdjustment();
            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;

        BUTTONS[0].updateInteractable(coolant_flow > 0.0f);
        BUTTONS[1].updateInteractable(coolant_flow < 1.0f);

        coolant_wheel.transform.GetChild(0).GetComponent<Renderer>().material = lit_neon;
        engine_coolant_supply_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;

        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);

        coolant_wheel.transform.GetChild(0).GetComponent<Renderer>().material = unlit_neon;
        engine_coolant_supply_display.SetActive(false);

        //return the wheel to 0 position
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitCoolantFlowAdjustmentRPC(float cf)
    {
        coolant_flow = cf;
        transform.GetComponent<PowerControl>().power_manager.controlPowerChange(2, this.GetType().Name, cf * MAX_POWER_CONSUMPTION);
        displayCoolantFlowAdjustment();

        if (NetworkManager.Singleton.IsHost == true)
        {
            if (engine_temperature_increase_coroutine == null && coolant_flow > 0.4f)
            {
                engine_temperature = 0.99f;
                transmitEngineTemperatureChangeRPC(engine_temperature);
                engine_temperature_increase_coroutine = StartCoroutine(engineTemperatureIncreaser());

                if (engine_speed_change_coroutine != null)
                {
                    StopCoroutine(engine_speed_change_coroutine);
                }
                engine_speed_change_coroutine = StartCoroutine(engineSpeedChange(1.0f));
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEngineTemperatureChangeRPC(float et)
    {
        engine_temperature = et;
        displayEngineTemperatureAdjustment();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEngineOverheatRPC()
    {
        engine_temperature = 1.0f;
        displayEngineTemperatureAdjustment();

        if (NetworkManager.Singleton.IsHost == true)
        {
            if (engine_speed_change_coroutine != null)
            {
                StopCoroutine(engine_speed_change_coroutine);
            }

            engine_speed_change_coroutine = StartCoroutine(engineSpeedChange(1.0f / IMPULSE_SPEED_CHANGE_FACTOR));
        }
    }
}