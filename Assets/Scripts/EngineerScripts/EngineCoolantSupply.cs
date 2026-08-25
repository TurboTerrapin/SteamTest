/*
    EngineCoolantSupply.cs
    - Handles turning of wheel to increase coolant and reduce engine temperature
    - Increases engine temperature over time
    - Tells PilotingSystem to reduce speed when engines are overheated
    Contributor(s): Jake Schott
    Last Updated: 8/10/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EngineCoolantSupply : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float TURN_SPEED = 0.25f;
    private static float IMPULSE_SPEED_CHANGE_FACTOR = 4.0f; //goes 1/4 as fast when engines are overheated
    private static float ENGINE_TEMPERATURE_INCREASE_SPEED = 0.005f;
    private static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles
    private static Color[] COLOR_OPTIONS = new Color[] { new Color(0.0f, 0.84f, 1.0f), new Color(1.0f, 0.47f, 0.0f), new Color(1.0f, 0.0f, 0.0f)}; //blue, orange, red

    private string CONTROL_NAME = "ENGINE COOLANT SUPPLY";
    private static string INFO_MESSAGE = "Regulates engines to prevent overheating and engine slowdown.";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject engine_coolant_supply_display;
    public GameObject coolant_wheel;
    public List<AudioClip> engine_capacity_notifications = null;
    private GameObject coolant_circle; //the UI section that shows the engine coolant flow
    private GameObject temperature; //the UI section that shows the engine temperature

    private ShipMovement ship_movement;
    private EngineMonitoring engine_monitoring;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private float coolant_flow = 0.0f;
    private float engine_temperature = 0.0f;
    private Coroutine engine_temperature_increase_coroutine = null;

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public List<GameObject> hand_placements = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private bool increasing = false;

    private void Start()
    {
        ship_movement = ReferenceAssistor.Instance.spaceship.GetComponent<ShipMovement>();
        engine_monitoring = ReferenceAssistor.Instance.module_handlers[0].GetComponent<EngineMonitoring>();

        coolant_circle = engine_coolant_supply_display.transform.GetChild(0).gameObject;
        temperature = engine_coolant_supply_display.transform.GetChild(1).gameObject;

        hud_info = new HUDInfo(CONTROL_NAME, true, MAX_POWER_CONSUMPTION);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false)); //increase button
        hud_info.setButtons(BUTTONS, 7);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public Transform getIKTarget(GameObject current_target)
    {
        float shortestDistance;
        int shortestIndex = 0;
        if (increasing)
        {
            shortestDistance = Vector3.Distance(hand_placements[1].transform.position, IK_targets[0].transform.position);
            for (int i = 1; i < IK_targets.Count; i++)
            {
                float distance = Vector3.Distance(hand_placements[1].transform.position, IK_targets[i].transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    shortestIndex = i;
                }
            }
        }
        else
        {
            shortestDistance = Vector3.Distance(hand_placements[1].transform.position, IK_targets[0].transform.position);
            for (int i = 1; i < IK_targets.Count; i++)
            {
                float distance = Vector3.Distance(hand_placements[1].transform.position, IK_targets[i].transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    shortestIndex = i;
                }
            }
        }
        return IK_targets[shortestIndex].transform;
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

    public Vector3 getRightHandOffset()
    {
        return right_hand_offset;
    }

    public float getLerpSpeed()
    {
        return lerp_speed;
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
        coolant_circle.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = coolant_flow;
        coolant_circle.transform.GetChild(2).GetComponent<UnityEngine.UI.Image>().fillAmount = coolant_flow;
        coolant_circle.transform.GetChild(3).transform.localRotation = Quaternion.Euler(180.0f, 0.0f, coolant_flow * 1080.0f);
    }

    private void displayEngineTemperatureAdjustment()
    {
        //display engine temperature
        Color status_color = COLOR_OPTIONS[0];
        if (engine_temperature >= 1.0f)
        {
            status_color = COLOR_OPTIONS[2];
        }
        else if (engine_temperature > 0.5)
        {
            status_color = COLOR_OPTIONS[1];
        }

        temperature.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = engine_temperature;
        temperature.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color = status_color;
        foreach (Transform t in temperature.transform.GetChild(1))
        {
            t.GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        }
        status_color.a = 0.08f;
        temperature.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
    }

    IEnumerator engineTemperatureIncreaser()
    {
        float coolant_flow_booster = 0.0f; //used to help accelerate temperature reduction
        while (true)
        {
            coolant_flow_booster = Mathf.Max(0.0f, Mathf.Min(3.0f, coolant_flow_booster + ((coolant_flow - 0.5f) * Time.deltaTime)));
            float difference = ENGINE_TEMPERATURE_INCREASE_SPEED - (coolant_flow * (ENGINE_TEMPERATURE_INCREASE_SPEED * (1.5f + coolant_flow_booster)));
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.isActiveAndEnabled == true)
            {
                if (difference > 0.0f)
                {
                    transmitEngineTemperatureChangeRPC(Mathf.Min(1.0f, engine_temperature + (difference * Time.deltaTime)));
                }
                else
                {
                    transmitEngineTemperatureChangeRPC(Mathf.Max(0.0f, engine_temperature + (difference * Time.deltaTime)));
                }
            }

            yield return null;
        }
    }

    public float getMaxImpulseSpeedBasedOnEngineTemperature()
    {
        return Mathf.Lerp(1.0f, 1.0f / IMPULSE_SPEED_CHANGE_FACTOR, engine_temperature);
    }

    public float getEngineTemperature()
    {
        return Mathf.Max(0.02f, engine_temperature);
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
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to to increase
        {
            turn_direction += 1;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))  //Q to decrease
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

        coolant_wheel.transform.GetChild(0).GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_neon;
        engine_coolant_supply_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;

        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        hud_info.setPowerConsumption(0.0f);

        coolant_wheel.transform.GetChild(0).GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_neon;
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
        ReferenceAssistor.Instance.power_manager.controlPowerChange(2, this.GetType().Name, cf * MAX_POWER_CONSUMPTION);
        hud_info.setPowerConsumption(cf * MAX_POWER_CONSUMPTION);
        displayCoolantFlowAdjustment();
        if (coolant_flow > 0.5f && engine_temperature > 0.5f)
        {
            ReferenceAssistor.Instance.hints_manager.removeHint("INCREASE COOLANT", 2);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEngineTemperatureChangeRPC(float et)
    {
        //handle notifications
        if (coolant_flow < 0.5f)
        {
            if (engine_temperature < 0.5f && et >= 0.5f)
            {
                ReferenceAssistor.Instance.audio_manager.AddNotification(0, engine_capacity_notifications[0]);
                ReferenceAssistor.Instance.audio_manager.AddNotification(0, engine_capacity_notifications[2]);
            }
            else if (engine_temperature < 1.0f && et >= 1.0f)
            {
                ReferenceAssistor.Instance.audio_manager.AddNotification(0, engine_capacity_notifications[1]);
                ReferenceAssistor.Instance.audio_manager.AddNotification(0, engine_capacity_notifications[2]);
            }
            if (et > 0.5f)
            {
                ReferenceAssistor.Instance.hints_manager.addHint("INCREASE COOLANT", 2);
            }
        }

        //set new engine temperature and display
        engine_temperature = et;
        if (is_powered == true)
        {
            displayEngineTemperatureAdjustment();
        }

        //if host, adjust maximum impulse speed
        if (NetworkManager.Singleton.IsHost == true)
        {
            ship_movement.AdjustMaxImpulseSpeed(getMaxImpulseSpeedBasedOnEngineTemperature());
        }

        //update pilot station
        engine_monitoring.temperatureAdjustment();
    }
}