/*
    TransmissionHandler.cs
    - Moves the waves
    - Switches waves
    - Updates frequency text
    - Handles the actual receiving/broadcasting of UniversalCommunicator messages
    Contributor(s): Jake Schott
    Last Updated: 6/28/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TransmissionHandler : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float WAVE_SPEED = 0.05f;
    private static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles
    private static int FREQUENCY_COUNT = 12; //the # of frequency options
    public static int[] FREQUENCY_RANGES = new int[2] { 1200, 1400 }; //currently 120.0 to 140.0

    public GameObject frequency_display;
    public GameObject wave_display;
    public GameObject input_option_display;
    public GameObject output_option_display;
    public GameObject success_indicator;
    public GameObject failure_indicator;
    public GameObject progress_indicators;
    public AudioSource transmission_processing_sound;
    public AudioSource transmission_success_sound;
    public AudioSource transmission_failure_sound;
    public AudioClip transmission_detected_notification;

    private GameObject waves;
    private UnityEngine.UI.RawImage alert_indicator;
    private TMP_Text frequency_text;

    private UniversalCommunicator universal_communicator;
    private InputOutputToggle input_output_toggle;
    private SignalOptions signal_options;
    private FrequencyAdjuster frequency_adjuster;

    private List<FrequencyData> frequencies = new List<FrequencyData>();

    private float shift = 0.0f; //used for scan wave moving
    private int frequency_index = 0;
    private List<int> transmission_code_indexes = null;
    private List<int> transmission_is_numeric = null;
    private int transmission_color = -1;
    private Coroutine alert_indicator_coroutine = null;
    private Coroutine signal_transmission_coroutine = null;

    private struct FrequencyData
    {
        public float frequency; //ex. 120.6 MH
        public int corresponding_wave; //ex. 0 for empty wave (just a line)
    }

    private void Start()
    {
        waves = wave_display.transform.GetChild(0).gameObject;
        alert_indicator = frequency_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>();
        frequency_text = frequency_display.transform.GetChild(1).GetComponent<TMP_Text>();
        universal_communicator = GetComponent<UniversalCommunicator>();
        input_output_toggle = GetComponent<InputOutputToggle>();
        signal_options = GetComponent<SignalOptions>();
        frequency_adjuster = GetComponent<FrequencyAdjuster>();

        for (int i = 0; i < FREQUENCY_COUNT; i++)
        {
            FrequencyData fd = new FrequencyData();
            frequencies.Add(fd);
        }
    }

    //handles the infinite moving of the signal wave 
    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
        shift += dt * WAVE_SPEED;
        if (shift > 0.06f)
        {
            shift -= 0.06f;
        }
        for (int i = 0; i < 3; i++)
        {
            waves.transform.GetChild(i).localPosition = new Vector3(0f, -0.03f + (0.06f * i) - shift, 0f);
        }
    }

    //clears added frequencies, randomizes new frequencies and sets to 0, called by ScenarioManager
    public void resetFrequencies()
    {
        frequency_index = 0;

        if (alert_indicator_coroutine != null)
        {
            StopCoroutine(alert_indicator_coroutine);
            alert_indicator_coroutine = null;
        }
        alert_indicator.color = new Color(0.0f, 0.84f, 1.0f);

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

    //returns texture of wave from 0-6 index
    public Texture getWaveTextureFromIndex(int index)
    {
        return wave_display.transform.GetChild(1).GetChild(index).gameObject.GetComponent<UnityEngine.UI.RawImage>().mainTexture;
    }

    //finds a frequency that is an empty wave and replaces it with new frequency and wave combination
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
                        transmitIndexReadjustmentRPC(frequency_index);
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

    //returns the corresponding receiver as an IUniversalCommunicable component to receive transmissions (or null if none exists)
    private IUniversalCommunicable findReceiver()
    {
        GameObject scenario_handler = GameObject.FindGameObjectWithTag("ScenarioHandler");
        if (scenario_handler != null)
        {
            Component[] scenario_handler_components = scenario_handler.GetComponents<Component>();
            for (int i = 0; i < scenario_handler_components.Length; i++)
            {
                IUniversalCommunicable transmission_receiver = scenario_handler_components[i] as IUniversalCommunicable;
                if (transmission_receiver != null)
                {
                    return transmission_receiver;
                }
            }
        }
        return null;
    }

    //returns the corresponding sender as an IUniversalBroadcastable component to broadcast transmissions (or null if none exists)
    private IBroadcastable findSender()
    {
        GameObject scenario_handler = GameObject.FindGameObjectWithTag("ScenarioHandler");
        if (scenario_handler != null)
        {
            Component[] scenario_handler_components = scenario_handler.GetComponents<Component>();
            for (int i = 0; i < scenario_handler_components.Length; i++)
            {
                IBroadcastable transmission_sender = scenario_handler_components[i] as IBroadcastable;
                if (transmission_sender != null)
                {
                    return transmission_sender;
                }
            }
        }
        return null;
    }

    //updates the frequency text (ex. 120.5Mhz) and which sprites are on the signal wave circle glass (ex. square wave)
    private void displayFrequencyAdjustment()
    {
        //update frequency text
        string freq_txt = frequencies[frequency_index].frequency.ToString();
        if (freq_txt.Contains(".") == false)
        {
            freq_txt += ".0";
        }
        frequency_text.SetText(freq_txt + "MH");

        //update signal wave sprites
        for (int i = 0; i < 3; i++)
        {
            waves.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().texture = getWaveTextureFromIndex(frequencies[frequency_index].corresponding_wave);
        }
    }

    //returns true if currently transmitting
    public bool isTransmitting()
    {
        return (signal_transmission_coroutine != null);
    }

    //called by FrequencyAdjuster upon turning the dial to the right or left
    public void updateFrequency(int freq)
    {
        if (freq > frequencies.Count - 1)
        {
            freq = 0;
        }
        else if (freq < 0)
        {
            freq = frequencies.Count - 1;
        }
        frequency_index = freq;
        displayFrequencyAdjustment();
    }

    //returns the current frequency index
    public int getCurrentFrequencyIndex()
    {
        return frequency_index;
    }

    //resets all progress indicators to default
    private void resetProgressIndicators()
    {
        foreach (Transform light in progress_indicators.transform) 
        {
            light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_neon;
        }
    }

    //changes the alpha of an input/output option glass
    private void displayTransmissionStatus(int index, float a)
    {
        Transform to_update = input_option_display.transform;
        if (index == 1)
        {
            to_update = output_option_display.transform;
        }
        to_update.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
        to_update.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
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

    //the process of either broadcasting or receiving a transmission
    IEnumerator signalTransmission(int index)
    {
        //play processing sound
        transmission_processing_sound.Play();

        //start by flashing progress lights for a little while
        for (int k = 0; k < 8; k++)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int x = i * 4; x < (i * 4) + 4; x++)
                {
                    if (UnityEngine.Random.Range(0, 2) == 0)
                    {
                        progress_indicators.transform.GetChild(x).GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_neon;
                    }
                }
                yield return new WaitForSeconds(0.05f);
                resetProgressIndicators();
            }
            if (index == 1) //broadcast, remove circles
            {
                universal_communicator.message_preview_display.transform.GetChild(7 - k).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
            }
            else //receive, add circles
            {
                //msg_preview_display.transform.GetChild(1 + k).gameObject.SetActive(true);
            }
        }

        bool successful_transmission = false;
        if (index == 1) //broadcast
        {
            IUniversalCommunicable transmission_receiver = findReceiver();
            if (transmission_receiver != null)
            {
                successful_transmission = transmission_receiver.checkTransmission(frequency_index, transmission_code_indexes, transmission_is_numeric, transmission_color);
                if (NetworkManager.Singleton.IsHost == true)
                {
                    transmission_receiver.handleTransmission(frequency_index, transmission_code_indexes, transmission_is_numeric, transmission_color); //SHOULD ONLY BE HANDLED BY THE HOST
                }
            }
        }
        else //receive
        {
            IBroadcastable transmission_sender = findSender();
            if (transmission_sender != null)
            {
                successful_transmission = transmission_sender.canFetchTransmission(frequency_index);
                transmission_sender.fetchTransmission(frequency_index);
            }
        }

        //stop sound
        transmission_processing_sound.Stop();

        if (successful_transmission == true)
        {
            success_indicator.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_green;
            transmission_success_sound.Play();
        }
        else
        {
            failure_indicator.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_red;
            transmission_failure_sound.Play();
        }

        yield return new WaitForSeconds(0.5f);

        //stop consuming power
        ReferenceAssistor.Instance.power_manager.controlPowerChange(1, this.GetType().Name, 0.0f);
        signal_options.getHUDinfo().setPowerConsumption(0.0f);

        //reset warning lights
        success_indicator.GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_green;
        failure_indicator.GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_red;

        //reset dial glass
        displayTransmissionStatus(index, 0.2f);

        activate();
        if (index == 1)
        {
            universal_communicator.enableKeyboard();
            universal_communicator.onInputChange();
        }
        input_output_toggle.activate();

        signal_transmission_coroutine = null;
    }

    //called by UniversalCommunicator and this
    public void activate()
    {
        frequency_display.SetActive(true);
        wave_display.SetActive(true);
        input_option_display.SetActive(true);
        output_option_display.SetActive(true);
        signal_options.activate();
        frequency_adjuster.activate();
    }

    //called by UniversalCommunicator on power off
    public void deactivate()
    {
        frequency_display.SetActive(false);
        wave_display.SetActive(false);
        input_option_display.SetActive(false);
        output_option_display.SetActive(false);
        displayTransmissionStatus(0, 0.2f);
        displayTransmissionStatus(1, 0.2f);
        signal_options.deactivate();
        frequency_adjuster.deactivate();

        if (signal_transmission_coroutine != null)
        {
            StopCoroutine(signal_transmission_coroutine);
            signal_transmission_coroutine = null;
            transmission_processing_sound.Stop();
            signal_options.getHUDinfo().setPowerConsumption(0.0f);
            ReferenceAssistor.Instance.power_manager.controlPowerChange(1, this.GetType().Name, 0.0f);
            resetProgressIndicators();
            success_indicator.GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_green;
            failure_indicator.GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_red;
        }
    }

    //called by SignalOptions
    public void transmitSignal(int index) //0 is receive, 1 is broadcast
    {
        if (index == 0) //receive
        {
            transmitSignalTransmissionRPC(index, frequency_index, "", "", -1);
        }
        else //broadcast
        {
            transmitSignalTransmissionRPC(index,
                                          frequency_index,
                                          DataConverter.listToString(universal_communicator.getCodeIndexes()),
                                          DataConverter.listToString(universal_communicator.getCodeIsSymbol()),
                                          universal_communicator.getCodeColor()
                                          );
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSignalTransmissionRPC(int index, int freq, string s_code_indexes, string s_is_numeric, int color)
    {
        //prevent manipulation during signal transmission
        frequency_adjuster.deactivate();
        signal_options.deactivate();
        signal_options.returnDials();

        //if broadcasting, store the message locally in TransmissionHandler
        if (index == 1) //broadcast
        {
            transmission_code_indexes = DataConverter.stringToList(s_code_indexes);
            transmission_is_numeric = DataConverter.stringToList(s_is_numeric);
            transmission_color = color;
        }

        if (index == 0) //receive
        {
            universal_communicator.clearMessagePreview();
        }

        if (universal_communicator.getInputMode() == true && index == 0)
        {
            input_output_toggle.forceSwitch(false);
        }
        input_output_toggle.deactivate();

        //update power consumption
        ReferenceAssistor.Instance.power_manager.controlPowerChange(1, this.GetType().Name, MAX_POWER_CONSUMPTION);
        signal_options.getHUDinfo().setPowerConsumption(MAX_POWER_CONSUMPTION);

        //update glass next to signal dial
        displayTransmissionStatus(index, 1.0f);

        //begin animation/transmission process
        if (signal_transmission_coroutine != null)
        {
            StopCoroutine(signal_transmission_coroutine);
        }
        signal_transmission_coroutine = StartCoroutine(signalTransmission(index));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitIndexReadjustmentRPC(int index)
    {
        frequency_index = index;
        displayFrequencyAdjustment();
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
            displayFrequencyAdjustment();
        }
        
        //check if need to alert
        if (cw != 0 && alert_indicator_coroutine == null)
        {
            ReferenceAssistor.Instance.audio_manager.AddNotification(0, transmission_detected_notification);
            alert_indicator.color = new Color(1.0f, 0.47f, 0.0f);
            alert_indicator_coroutine = StartCoroutine(alertIndicatorFlasher());
        }
    }
}