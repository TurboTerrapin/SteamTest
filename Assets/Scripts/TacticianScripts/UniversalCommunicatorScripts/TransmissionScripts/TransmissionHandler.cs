/*
    TransmissionHandler.cs
    - Moves the waves
    - Switches waves
    - Updates frequency text
    - Handles the actual receiving/broadcasting of UniversalCommunicator messages
    Contributor(s): Jake Schott
    Last Updated: 7/24/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TransmissionHandler : NetworkBehaviour
{
    //CLASS CONSTANTS
    public static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles (power consumption during signal transmission)

    public GameObject input_option_display;
    public GameObject output_option_display;
    public GameObject success_indicator;
    public GameObject failure_indicator;
    public GameObject progress_indicators;
    public AudioSource transmission_processing_sound;
    public AudioSource transmission_success_sound;
    public AudioSource transmission_failure_sound;

    private UniversalCommunicator universal_communicator;
    private InputOutputToggle input_output_toggle;
    private SignalOptions signal_options;
    private FrequencyAdjuster frequency_adjuster;

    private List<int> transmission_code_indexes = null;
    private List<int> transmission_is_numeric = null;
    private List<int> transmission_colors = null;
    private Coroutine signal_transmission_coroutine = null;

    private void Start()
    {
        universal_communicator = GetComponent<UniversalCommunicator>();
        input_output_toggle = GetComponent<InputOutputToggle>();
        signal_options = GetComponent<SignalOptions>();
        frequency_adjuster = GetComponent<FrequencyAdjuster>();
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

    //returns true if currently transmitting
    public bool isTransmitting()
    {
        return (signal_transmission_coroutine != null);
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

    //the process of either broadcasting or receiving a transmission
    IEnumerator signalTransmission(int index, float frequency)
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
                universal_communicator.message_preview_display.transform.GetChild(7 - k).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.08f);
            }
            else //receive, add circles
            {
                universal_communicator.message_preview_display.transform.GetChild(k).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
            }
        }

        bool successful_transmission = false;
        if (index == 1) //broadcast
        {
            IUniversalCommunicable transmission_receiver = findReceiver();
            if (transmission_receiver != null)
            {
                successful_transmission = transmission_receiver.checkTransmission(frequency, transmission_code_indexes, transmission_is_numeric, transmission_colors[0]);
                if (NetworkManager.Singleton.IsHost == true)
                {
                    transmission_receiver.handleTransmission(frequency, transmission_code_indexes, transmission_is_numeric, transmission_colors[0]); //SHOULD ONLY BE HANDLED BY THE HOST
                }
            }
        }
        else if (index == 0) //receive
        {
            successful_transmission = (transmission_code_indexes.Count > 0);
            if (successful_transmission == true)
            {
                universal_communicator.displayOutputAdjustment(transmission_code_indexes, transmission_is_numeric, transmission_colors);
            }
            else
            {
                universal_communicator.displayOutputAdjustment(null, null, null);
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

        //reset glass next to dial
        displayTransmissionStatus(index, 0.08f);

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
        input_option_display.SetActive(true);
        output_option_display.SetActive(true);
        signal_options.activate();
        frequency_adjuster.activate();
    }

    //called by UniversalCommunicator on power off
    public void deactivate()
    {
        input_option_display.SetActive(false);
        output_option_display.SetActive(false);
        displayTransmissionStatus(0, 0.08f);
        displayTransmissionStatus(1, 0.08f);
        signal_options.deactivate();
        frequency_adjuster.deactivate(false);

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
            float frequency = frequency_adjuster.getCurrentFrequencyValue();
            string code_indexes = "";
            string is_numeric = "";
            string colors = "";

            IBroadcastable transmission_sender = findSender();
            if (transmission_sender != null)
            {
                if (transmission_sender.canFetchTransmission(frequency))
                {
                    UniversalCommunicatorCodeData data = transmission_sender.fetchTransmission(frequency);
                    code_indexes = DataConverter.arrayToString(data.getCodeIndexes());
                    is_numeric = DataConverter.arrayToString(data.getCodeIsNumeric());
                    colors = DataConverter.arrayToString(data.getCodeColors());
                }
            }

            transmitSignalTransmissionRPC(index, frequency, code_indexes, is_numeric, colors);
        }
        else //broadcast
        {
            int[] code_colors = new int[8];
            for (int i = 0; i < 8; i++)
            {
                code_colors[i] = universal_communicator.getCodeColor();
            }
            transmitSignalTransmissionRPC(index,
                                          frequency_adjuster.getCurrentFrequencyValue(),
                                          DataConverter.listToString(universal_communicator.getCodeIndexes()),
                                          DataConverter.listToString(universal_communicator.getCodeIsSymbol()),
                                          DataConverter.arrayToString(code_colors)
                                          );
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSignalTransmissionRPC(int index, float frequency, string s_code_indexes, string s_is_numeric, string s_code_colors)
    {
        //prevent manipulation during signal transmission
        frequency_adjuster.deactivate(true);
        signal_options.deactivate();
        signal_options.returnDials();

        //store the message locally in TransmissionHandler
        transmission_code_indexes = DataConverter.stringToList(s_code_indexes);
        transmission_is_numeric = DataConverter.stringToList(s_is_numeric);
        transmission_colors = DataConverter.stringToList(s_code_colors);

        if (index == 0) //receive
        {
            universal_communicator.clearMessagePreview();
        }

        if (universal_communicator.getInputMode() == true && index == 0)
        {
            input_output_toggle.forceSwitch(false);
        }
        else if (universal_communicator.getInputMode() == false && index == 1)
        {
            input_output_toggle.forceSwitch(true);
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
        signal_transmission_coroutine = StartCoroutine(signalTransmission(index, frequency));
    }
}