/*
    EnergyPattern.cs
    - Handles enabling/disabling energy pattern display
    - Handles shifting between ship/probe/tractor beam configuration
    Contributor(s): Jake Schott
    Last Updated: 3/2/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static AnimatorHandler;

public class EnergyPattern : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 1.0f; //how long it takes to turn on/off the energy pattern display
    private static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles
    private static float BAR_ANIMATION_TIME = 0.2f; //bars change every 0.2 seconds
    private static float ENABLED_BLINKER_REFRESH = 2.0f;

    private string[] CONTROL_NAMES = { "ENERGY PATTERN POWER", "ENERGY PATTERN VIEWER" };
    private List<string> INFO_MESSAGES = new List<string>() { "Enables/disables the energy pattern viewer used to analyze spatial anomalies." };
    private List<string> CONTROL_DESCS = new List<string>() { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[1] { new List<Button>() };

    public GameObject energy_pattern_dial;
    public GameObject energy_pattern_display;
    public GameObject energy_pattern_signal_display;
    public GameObject enabled_indicator;

    public List<Texture> center_options;
    public List<Texture> ring_options;
    public List<Texture> dot_options;
    public List<Color> color_options;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private PatternData corresponding_pattern_data = null;
    private bool display_enabled = false;
    private Coroutine alert_flasher_coroutine = null;
    private Coroutine signal_indicator_coroutine = null;
    private Coroutine energy_pattern_power_coroutine = null;

    private List<string> ray_targets = new List<string> { "energy_pattern_power", "energy_pattern_viewer" };

    private static HUDInfo[] hud_infos = new HUDInfo[2];

    [Header("IK Targetable Details")]
    public GameObject IK_target;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private void Start()
    {
        hud_infos[0] = new HUDInfo(CONTROL_NAMES[0], true);
        hud_infos[1] = new HUDInfo(CONTROL_NAMES[1], true);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        hud_infos[0].setButtons(BUTTON_LISTS[0], 6);
        hud_infos[0].setInfo(INFO_MESSAGES[0]);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        return hud_infos[index];
    }
    public Transform getIKTarget(GameObject current_target)
    {
        return IK_target.transform;
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
    private void handlePowerConsumptionChange()
    {
        if (display_enabled == true)
        {
            ReferenceAssistor.Instance.power_manager.controlPowerChange(2, this.GetType().Name, MAX_POWER_CONSUMPTION);
            hud_infos[0].setPowerConsumption(MAX_POWER_CONSUMPTION);
            hud_infos[1].setPowerConsumption(MAX_POWER_CONSUMPTION);
        }
        else
        {
            ReferenceAssistor.Instance.power_manager.controlPowerChange(2, this.GetType().Name, 0.0f);
            hud_infos[0].setPowerConsumption(0.0f);
            hud_infos[1].setPowerConsumption(0.0f);
        }
    }

    public void updateEnergyPatternDisplay()
    {
        //update energy pattern display
        energy_pattern_display.transform.GetChild(0).gameObject.SetActive(is_powered == true && display_enabled && corresponding_pattern_data != null);

        //update enabled indicator
        updateEnabledIndicator();
    }

    //establishes the pattern and sets the corresponding pattern data
    public void setPattern(PatternData pd)
    {
        //clear current pattern (if there is one)
        energy_pattern_display.GetComponent<PatternVisualizer>().resetPattern();

        corresponding_pattern_data = pd;
        energy_pattern_display.GetComponent<PatternVisualizer>().displayPattern(corresponding_pattern_data);
        updateSignalIndicator();
        energy_pattern_display.transform.GetChild(0).gameObject.SetActive(display_enabled);

        //handle orange blinker
        if (display_enabled == false && alert_flasher_coroutine == null)
        {
            alert_flasher_coroutine = StartCoroutine(alertFlasher());
        }
    }

    //clears the pattern
    public void clearPattern()
    {
        //resets to default state
        energy_pattern_display.GetComponent<PatternVisualizer>().resetPattern();
        corresponding_pattern_data = null;
        display_enabled = false;
        updateEnergyPatternDisplay();
        updateSignalIndicator();
    }

    //updates the colors in the PatternData and corresponding visualizer
    public void updateColors(List<int> new_ring_colors, int new_center_color, float anim_time)
    {
        energy_pattern_display.GetComponent<PatternVisualizer>().changeColors(new_ring_colors, new_center_color, anim_time);
    }

    //updates the colors in the PatternData and corresponding visualizer
    public void updateColors(int[] new_ring_colors, int new_center_color, float anim_time)
    {
        energy_pattern_display.GetComponent<PatternVisualizer>().changeColors(new_ring_colors, new_center_color, anim_time);
    }

    //resizes the pattern in the corresponding index to either contracted (true) or expanded (false) in time_interval time
    public void resizePattern(bool shrink, float time_interval)
    {
        if (shrink == true)
        {
            energy_pattern_display.GetComponent<PatternVisualizer>().contractPattern(time_interval);
        }
        else
        {
            energy_pattern_display.GetComponent<PatternVisualizer>().expandPattern(time_interval);
        }
    }

    //returns the data that informs the energy pattern
    public PatternData getPatternData()
    {
        return corresponding_pattern_data;
    }

    //sizes the bar at the index to the to_size_to input
    private void resizeBar(Transform line, float to_size_to)
    {
        line.GetComponent<RectTransform>().sizeDelta = new Vector2(0.006f + to_size_to * 2, 0.006f);
        line.GetChild(0).localPosition = new Vector3(0.004f + to_size_to, 0.0f, 0.0f);
        line.GetChild(1).localPosition = new Vector3(-0.004f - to_size_to, 0.0f, 0.0f);
    }

    //handles the increasing/decreasing bar animation on the screen above the energy pattern power dial
    IEnumerator sourceIndicatorAnimator()
    {
        int num_lines = energy_pattern_signal_display.transform.GetChild(0).childCount - 1;
        float[] starting_sizes = new float[num_lines];
        float[] sizes = new float[num_lines];
        GameObject lines = energy_pattern_signal_display.transform.GetChild(0).gameObject;
        bool reset = false;

        while (reset == false)
        {
            for (int i = 0; i < num_lines; i++)
            {
                starting_sizes[i] = lines.transform.GetChild(i + 1).GetChild(0).localPosition.x - 0.004f;
                if (corresponding_pattern_data != null || is_powered == false)
                {
                    sizes[i] = UnityEngine.Random.Range(0.0f, 1.0f) * 0.006f;
                }
                else
                {
                    sizes[i] = 0.0f;
                }
            }

            float anim_time = BAR_ANIMATION_TIME;
            while (anim_time > 0.0f)
            {
                float dt = Time.deltaTime;
                anim_time = Mathf.Max(0.0f, anim_time - dt);
                for (int i = 0; i < num_lines; i++)
                {
                    float to_size_to = Mathf.Lerp(starting_sizes[i], sizes[i], 1.0f - (anim_time / BAR_ANIMATION_TIME));
                    resizeBar(lines.transform.GetChild(i + 1), to_size_to);
                }
                yield return null;
            }

            //end loop if not active
            if (corresponding_pattern_data == null || is_powered == false)
            {
                reset = true;
                for (int i = 0; i < num_lines; i++)
                {
                    if (sizes[i] != 0.0f)
                    {
                        reset = false;
                        break;
                    }
                }
            }
        }

        signal_indicator_coroutine = null;
    }

    //flashes the orange blinker
    IEnumerator alertFlasher()
    {
        float elapsed_time = 0.0f;
        while (true)
        {
            elapsed_time += Time.deltaTime * ENABLED_BLINKER_REFRESH;
            float a = Mathf.Lerp(0.2f, 1.0f, Mathf.PingPong(elapsed_time, 1.0f));
            enabled_indicator.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.47f, 0.0f, a);

            yield return null;
        }
    }

    //updates the orange flasher
    private void updateEnabledIndicator()
    {
        if (is_powered == true && display_enabled == true)
        {
            if (alert_flasher_coroutine != null)
            {
                StopCoroutine(alert_flasher_coroutine);
                alert_flasher_coroutine = null;
            }
            enabled_indicator.GetComponent<SpriteRenderer>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        }
        else if (is_powered == true && display_enabled == false && corresponding_pattern_data != null)
        {
            if (alert_flasher_coroutine == null)
            {
                alert_flasher_coroutine = StartCoroutine(alertFlasher());
            }
        }
        else
        {
            if (alert_flasher_coroutine != null)
            {
                StopCoroutine(alert_flasher_coroutine);
                alert_flasher_coroutine = null;
            }
            enabled_indicator.GetComponent<SpriteRenderer>().color = new Color(0.0f, 0.84f, 1.0f, 0.0f);
        }
    }

    //updates the blue increasing/decreasing bar animation
    private void updateSignalIndicator()
    {
        if (corresponding_pattern_data != null && signal_indicator_coroutine == null && is_powered == true)
        {
            signal_indicator_coroutine = StartCoroutine(sourceIndicatorAnimator());
        }
    }

    IEnumerator powerChange()
    {
        bool enabling = !display_enabled;
        if (enabling == false)
        {
            display_enabled = false;
            handlePowerConsumptionChange();
            updateEnergyPatternDisplay();
            updateEnabledIndicator();
        }

        float anim_time = SWITCH_TIME;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            float switch_percentage = anim_time / SWITCH_TIME;
            if (enabling == true)
            {
                switch_percentage = 1.0f - switch_percentage;
            }

            energy_pattern_dial.transform.localRotation =
                Quaternion.Euler(energy_pattern_dial.transform.localEulerAngles.x,
                                 energy_pattern_dial.transform.localEulerAngles.y,
                                 Mathf.Lerp(180.0f, 90.0f, switch_percentage));

            yield return null;
        }

        if (enabling == true)
        {
            display_enabled = true;
            handlePowerConsumptionChange();
            updateEnergyPatternDisplay();
            BUTTON_LISTS[0][0].updateDesc(CONTROL_DESCS[1]);
        }
        else
        {
            BUTTON_LISTS[0][0].updateDesc(CONTROL_DESCS[0]);
        }
        BUTTON_LISTS[0][0].updateInteractable(true);

        energy_pattern_power_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false || energy_pattern_power_coroutine != null)
        {
            return;
        }

        if (ray_targets.IndexOf(current_target.name) == 1)
        {
            return;
        }

        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
        {
            BUTTON_LISTS[0][0].toggle(0.2f);
            transmitEnergyPatternPowerChangeRPC(display_enabled);
        }
    }

    public void resetToDefault()
    {
        clearPattern();
        updateEnergyPatternDisplay();
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float starting_rotation = energy_pattern_dial.transform.localRotation.eulerAngles.z;

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            energy_pattern_dial.transform.localRotation =
                Quaternion.Euler(energy_pattern_dial.transform.localEulerAngles.x,
                                 energy_pattern_dial.transform.localEulerAngles.y,
                                 Mathf.Lerp(starting_rotation, 180.0f, 1.0f - (anim_time / power_off_time)));

            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        updateEnergyPatternDisplay();
        updateSignalIndicator();
        energy_pattern_signal_display.SetActive(true);
        BUTTON_LISTS[0][0].updateInteractable(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        display_enabled = false;
        updateEnergyPatternDisplay();
        updateSignalIndicator();
        energy_pattern_signal_display.SetActive(false);
        BUTTON_LISTS[0][0].updateInteractable(false);
        BUTTON_LISTS[0][0].updateDesc(CONTROL_DESCS[0]);
        if (energy_pattern_power_coroutine != null)
        {
            StopCoroutine(energy_pattern_power_coroutine);
            energy_pattern_power_coroutine = null;
        }
        hud_infos[0].setPowerConsumption(0.0f);
        hud_infos[1].setPowerConsumption(0.0f);

        //return energy pattern dial to off
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEnergyPatternPowerChangeRPC(bool de)
    {
        display_enabled = de;
        if (energy_pattern_power_coroutine != null)
        {
            StopCoroutine(energy_pattern_power_coroutine);
        }
        energy_pattern_power_coroutine = StartCoroutine(powerChange());
    }
}