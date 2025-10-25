/*
    EngineCoolantSupply.cs
    - Handles turning of wheel to increase coolant and reduce engine temperature
    - Increases engine temperature over time
    - Tells PilotingSystem to reduce speed when engines are overheated
    Contributor(s): Jake Schott
    Last Updated: 10/23/2025
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class EngineCoolantSupply : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float TURN_SPEED = 0.15f;
    private static float IMPULSE_SPEED_CHANGE_FACTOR = 4.0f; //goes 1/4 as fast when engines are overheated
    private static float ENGINE_TEMPERATURE_INCREASE_SPEED = 0.005f;
    private static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles

    private string CONTROL_NAME = "ENGINE COOLANT SUPPLY";
    private static string INFO_MESSAGE = "Regulates engines to prevent overheating and engine slowdown.";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public Material lit_neon;
    public Material unlit_neon;

    public GameObject engine_coolant_supply_display;
    public GameObject coolant_wheel;
    private GameObject flow; //the UI section that shows the engine coolant flow
    private GameObject temperature; //the UI section that shows the engine temperature
    private GameObject capacity; //the UI section that shows impulse capacity

    private PilotingSystem piloting_system;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private float coolant_flow = 0.0f;
    private float engine_temperature = 0.0f;
    private Coroutine engine_temperature_increase_coroutine = null;

    private static HUDInfo hud_info = null;

    private void Start()
    {
        piloting_system = GameObject.FindGameObjectWithTag("Spaceship").GetComponent<PilotingSystem>();

        flow = engine_coolant_supply_display.transform.GetChild(0).gameObject;
        temperature = engine_coolant_supply_display.transform.GetChild(1).gameObject;
        capacity = engine_coolant_supply_display.transform.GetChild(2).gameObject;

        hud_info = new HUDInfo(CONTROL_NAME, true);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false)); //increase button
        hud_info.setButtons(BUTTONS, 7);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public void resetToDefault()
    {
        if (engine_temperature_increase_coroutine != null)
        {
            StopCoroutine(engine_temperature_increase_coroutine);
            engine_temperature_increase_coroutine = null;
        }
        engine_temperature = 0.0f;
        displayEngineTemperatureAdjustment();
    }

    private void displayCoolantFlowAdjustment()
    {
        //update wheel rotation
        coolant_wheel.transform.localRotation = Quaternion.Euler(-54.0f, 315.0f, coolant_flow * 1080.0f);

        //update screen wheel
        flow.transform.GetChild(2).GetComponent<UnityEngine.UI.Image>().fillAmount = coolant_flow;
        flow.transform.GetChild(3).transform.localRotation = Quaternion.Euler(180.0f, 0.0f, coolant_flow * 1080.0f);
    }

    private void displayEngineTemperatureAdjustment()
    {
        //display impulse capacity
        float impulse_capacity = Mathf.Lerp(1.0f, 1.0f / IMPULSE_SPEED_CHANGE_FACTOR, engine_temperature);
        string impulse_capacity_as_string = ((Mathf.Round(impulse_capacity * 1000.0f) / 1000.0f) * 100.0f).ToString();
        if (impulse_capacity_as_string.Contains('.') == false)
        {
            impulse_capacity_as_string += ".0";
        }
        capacity.transform.GetChild(1).GetComponent<TMP_Text>().SetText(impulse_capacity_as_string + "%");
        capacity.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().fillAmount = impulse_capacity;

        //display engine temperature
        Color status_color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        if (engine_temperature >= 1.0f)
        {
            status_color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
        else if (engine_temperature > 0.5)
        {
            status_color = new Color(0.84f, 0.62f, 0.0f, 1.0f);
        }

        float engine_temp = Mathf.Max(0.02f, engine_temperature);
        temperature.transform.GetChild(0).GetComponent<TMP_Text>().color = status_color;
        temperature.transform.GetChild(2).transform.localPosition = new Vector3(Mathf.Lerp(-0.011f, 0.063f, engine_temp), 0.0195f, 0.0f);
        temperature.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        temperature.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().fillAmount = engine_temp;
        temperature.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().color = status_color;
    }

    IEnumerator engineTemperatureIncreaser()
    {
        float coolant_flow_booster = 0.0f; //used to help accelerate temperature reduction
        while (true)
        {
            coolant_flow_booster = Mathf.Max(0.0f, Mathf.Min(2.0f, coolant_flow_booster + ((coolant_flow - 0.5f) * Time.deltaTime)));
            float difference = ENGINE_TEMPERATURE_INCREASE_SPEED - (coolant_flow * (ENGINE_TEMPERATURE_INCREASE_SPEED * (1.5f + coolant_flow_booster)));
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
            piloting_system.AdjustMaxImpulseSpeed(Mathf.Lerp(1.0f, 1.0f / IMPULSE_SPEED_CHANGE_FACTOR, engine_temperature));
            yield return null;
        }
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
        hud_info.setPowerConsumption(0.0f);

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
        hud_info.setPowerConsumption(cf * MAX_POWER_CONSUMPTION);
        displayCoolantFlowAdjustment();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEngineTemperatureChangeRPC(float et)
    {
        engine_temperature = et;
        displayEngineTemperatureAdjustment();
    }
}