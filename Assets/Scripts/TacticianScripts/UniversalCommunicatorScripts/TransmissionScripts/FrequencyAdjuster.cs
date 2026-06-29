/*
    FrequencyAdjuster.cs
    - Switches frequencies
    Contributor(s): Jake Schott
    Last Updated: 6/28/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class FrequencyAdjuster : NetworkBehaviour, IControllable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 70.0f; //for dial
    private static float WAVE_SPEED = 0.05f; //horizontal wave movement
    private static float FREQUENCY_SWITCH_SPEED = 3.5f; //for frequency
    private static int FREQUENCY_COUNT = 12; //the # of frequency options
    public static int[] FREQUENCY_RANGES = new int[2] { 1200, 1400 }; //currently 120.0 to 140.0

    private string CONTROL_NAME = "FREQUENCY ADJUSTER";
    private static string INFO_MESSAGE = "Adjusts the frequency for universal communicator transmissions.";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject transmission_frequency_display;
    public GameObject transmission_wave_display;
    public GameObject transmission_frequency_dial;
    public List<Texture> transmission_wave_options = null;
    public AudioSource transmission_frequency_switch_boop;
    public AudioClip transmission_detected_notification;

    private GameObject moving_waves;
    private UnityEngine.UI.RawImage alert_indicator;
    private TMP_Text frequency_text;
    private TransmissionHandler transmission_handler;

    private List<FrequencyData> frequencies = new List<FrequencyData>();
    private bool is_active = false;
    private float shift = 0.0f; //used for horizontal wave movement
    private int frequency_index = 0;
    private float dial_rotation = 0.0f;
    private float frequency_update_progress = 0.5f; //increases at 1.0, decreases at 0.0
    private Coroutine alert_indicator_coroutine = null;
    private Coroutine wave_mover_coroutine = null;

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public GameObject IK_target;
    public AnimatorHandler.HandInteractionType hand_interaction_type;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private struct FrequencyData
    {
        public float frequency; //ex. 120.6 MH
        public int corresponding_wave; //ex. 0 for empty wave (just a line)
    }

    private void Start()
    {
        moving_waves = transmission_wave_display.transform.GetChild(0).gameObject;
        alert_indicator = transmission_frequency_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>();
        frequency_text = transmission_frequency_display.transform.GetChild(1).GetComponent<TMP_Text>();
        transmission_handler = GetComponent<TransmissionHandler>();  

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

        for (int i = 0; i < FREQUENCY_COUNT; i++)
        {
            FrequencyData fd = new FrequencyData();
            frequencies.Add(fd);
        }
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
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

    //returns the current frequency index
    public int getCurrentFrequencyIndex()
    {
        return frequency_index;
    }

    //returns the current frequency float value
    public float getCurrentFrequencyValue()
    {
        return frequencies[frequency_index].frequency;
    }

    //clears added frequencies, randomizes new frequencies and sets to 0, called by ScenarioManager
    public void resetFrequencies()
    {
        frequency_index = 0;

        if (NetworkManager.Singleton.IsHost == true)
        {
            List<float> new_frequencies = new List<float>();
            for (int i = 0; i < FREQUENCY_COUNT; i++)
            {
                float to_add = UnityEngine.Random.Range(FREQUENCY_RANGES[0], FREQUENCY_RANGES[1] + 1) / 10.0f;
                while (new_frequencies.Contains(to_add) == true)
                {
                    to_add = UnityEngine.Random.Range(FREQUENCY_RANGES[0], FREQUENCY_RANGES[1] + 1) / 10.0f;
                }
                new_frequencies.Add(to_add);
            }
            new_frequencies.Sort();
            for (int i = 0; i < new_frequencies.Count; i++)
            {
                transmitFrequencyUpdateRPC(i, new_frequencies[i], 0);
            }
        }
    }

    //flashes orange alert indicator
    IEnumerator alertIndicatorFlasher()
    {
        float elapsed_time = 0.0f;
        while (true)
        {
            elapsed_time += Time.deltaTime * 2.0f;
            float a = Mathf.Lerp(0.2f, 1.0f, Mathf.PingPong(elapsed_time, 1.0f));
            alert_indicator.GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 0.47f, 0.0f, a);

            yield return null;
        }
    }

    //turns dial and updates the frequency text (ex. 120.5MH) and which sprites are on the signal wave circle glass (ex. square wave)
    private void displayAdjustment()
    {
        //rotate dial
        transmission_frequency_dial.transform.localRotation = Quaternion.Euler(248.0f, 0.0f, dial_rotation);

        //update frequency text
        string freq_txt = frequencies[frequency_index].frequency.ToString();
        if (freq_txt.Contains(".") == false)
        {
            freq_txt += ".0";
        }
        frequency_text.SetText(freq_txt + "MH");

        //update moving wave sprites
        for (int i = 0; i < 3; i++)
        {
            moving_waves.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().texture = transmission_wave_options[frequencies[frequency_index].corresponding_wave];
        }

        //move wave sprites if not moving
        if (wave_mover_coroutine == null && frequencies[frequency_index].corresponding_wave != 0)
        {
            wave_mover_coroutine = StartCoroutine(horizontalWaveMover());
        }
    }

    //handles the infinite moving of the signal wave 
    IEnumerator horizontalWaveMover()
    {
        while (frequencies[frequency_index].corresponding_wave != 0)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            shift += dt * WAVE_SPEED;
            if (shift > 0.06f)
            {
                shift -= 0.06f;
            }
            for (int i = 0; i < 3; i++)
            {
                moving_waves.transform.GetChild(i).localPosition = new Vector3(0f, -0.03f + (0.06f * i) - shift, 0f);
            }

            yield return null;
        }

        wave_mover_coroutine = null;
    }

    //finds either matching frequency, or a frequency that is an empty wave and replaces it with new frequency and wave combination
    public void frequencyReplacement(float freq, int cw)
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            //determine which index to replace
            List<float> curr_frequencies = new List<float>();
            List<int> dummy_candidates = new List<int>();
            for (int i = 0; i < FREQUENCY_COUNT; i++)
            {
                if (frequencies[i].frequency == freq)
                {
                    transmitFrequencyUpdateRPC(i, freq, cw);
                    return;
                }
                if (frequencies[i].corresponding_wave == 0 && i != frequency_index)
                {
                    dummy_candidates.Add(i);
                }
                curr_frequencies.Add(frequencies[i].frequency);
            }

            //find a dummy frequency to replace
            int to_replace_index = dummy_candidates[UnityEngine.Random.Range(0, dummy_candidates.Count)];
            curr_frequencies[to_replace_index] = freq;
            curr_frequencies.Sort();

            //replace the dummy frequency
            FrequencyData fd = frequencies[to_replace_index];
            fd.frequency = freq;
            fd.corresponding_wave = cw;
            frequencies[to_replace_index] = fd;

            //re-sort
            float current_frequency = frequencies[frequency_index].frequency;
            List<FrequencyData> to_reorganize = new List<FrequencyData>();
            for (int i = 0; i < FREQUENCY_COUNT; i++)
            {
                FrequencyData to_add = new FrequencyData();
                to_add.frequency = curr_frequencies[i];
                for (int x = 0; x < FREQUENCY_COUNT; x++)
                {
                    if (frequencies[x].frequency == curr_frequencies[i])
                    {
                        to_add.corresponding_wave = frequencies[x].corresponding_wave;
                    }
                }
                to_reorganize.Add(to_add);
            }

            //readjust index if current frequency got shifted
            if (to_reorganize[frequency_index].frequency != current_frequency)
            {
                for (int i = 0; i < FREQUENCY_COUNT; i++)
                {
                    if (to_reorganize[i].frequency == current_frequency)
                    {
                        frequency_index = i;
                        transmitFrequencyDialAdjustmentRPC(dial_rotation, frequency_update_progress, frequency_index);
                        break;
                    }
                }
            }

            //transmit new list
            for (int i = 0; i < FREQUENCY_COUNT; i++)
            {
                if (frequencies[i].frequency != to_reorganize[i].frequency || frequencies[i].corresponding_wave != to_reorganize[i].corresponding_wave)
                {
                    transmitFrequencyUpdateRPC(i, to_reorganize[i].frequency, to_reorganize[i].corresponding_wave);
                }
            }
        }
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_active == false || transmission_handler.isTransmitting() == true)
        {
            return;
        }

        int dial_direction = 0;
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to increase
        {
            dial_direction += 1;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //Q to decrease
        {
            dial_direction -= 1;
        }
        int freq_index = frequency_index;
        float freq_update_progress = frequency_update_progress;
        if (dial_direction != 0)
        {
            if (dial_direction > 0)
            {
                dial_rotation += dt * TURN_TIME;
                freq_update_progress += dt * FREQUENCY_SWITCH_SPEED;
            }
            else
            {
                dial_rotation -= dt * TURN_TIME;
                freq_update_progress -= dt * FREQUENCY_SWITCH_SPEED;
            }
            if (frequency_update_progress >= 1.0f)
            {
                freq_update_progress -= 1.0f;
                freq_index++;
                if (freq_index > FREQUENCY_COUNT - 1)
                {
                    freq_index = 0;
                }
            }
            else if (frequency_update_progress <= 0.0f)
            {
                freq_update_progress += 1.0f;
                freq_index--;
                if (freq_index < 0)
                {
                    freq_index = FREQUENCY_COUNT - 1;
                }
            }
            if (dial_rotation > 360.0f)
            {
                dial_rotation -= 360.0f;
            }
            else if (dial_rotation > 0.0f)
            {
                dial_rotation += 360.0f;
            }
            transmitFrequencyDialAdjustmentRPC(dial_rotation, freq_update_progress, freq_index);
        }
    }

    //checks if alert flash is necessary and starts/stops it
    private void updateAlertStatus()
    {
        bool alert_necessary = false;
        if (is_active == true)
        {
            foreach (FrequencyData f in frequencies)
            {
                if (f.corresponding_wave != 0)
                {
                    alert_necessary = true;
                    break;
                }
            }
        }

        if (alert_necessary == true && alert_indicator_coroutine == null)
        {
            ReferenceAssistor.Instance.audio_manager.AddNotification(0, transmission_detected_notification);
            alert_indicator.color = new Color(1.0f, 0.47f, 0.0f);
            alert_indicator_coroutine = StartCoroutine(alertIndicatorFlasher());
        }
        else if (alert_necessary == false && alert_indicator_coroutine != null)
        {
            StopCoroutine(alert_indicator_coroutine);
            alert_indicator_coroutine = null;
            alert_indicator.color = new Color(0.0f, 0.84f, 1.0f);
        }
    }

    public void activate()
    {
        is_active = true;
        transmission_frequency_display.SetActive(true);
        transmission_wave_display.SetActive(true);
        BUTTONS[0].updateInteractable(true);
        BUTTONS[1].updateInteractable(true);
        updateAlertStatus();
    }

    public void deactivate(bool stay_visible)
    {
        is_active = false;
        transmission_frequency_display.SetActive(stay_visible);
        transmission_wave_display.SetActive(stay_visible);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        if (stay_visible == false)
        {
            updateAlertStatus();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitFrequencyDialAdjustmentRPC(float dial_rot, float freq_update_progress, int freq_index)
    {
        if (frequency_index != freq_index && frequency_update_progress != freq_update_progress)
        {
            transmission_frequency_switch_boop.Play();
        }
        dial_rotation = dial_rot;
        frequency_index = freq_index;
        frequency_update_progress = freq_update_progress;
        displayAdjustment();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitFrequencyUpdateRPC(int index, float freq, int cw)
    {
        //find index slot and set new frequency float value and new wave int value
        FrequencyData to_set = frequencies[index];
        to_set.frequency = freq;
        to_set.corresponding_wave = cw;
        frequencies[index] = to_set;
        if (index == frequency_index)
        {
            displayAdjustment();
        }

        //check if need to alert
        updateAlertStatus();
    }
}