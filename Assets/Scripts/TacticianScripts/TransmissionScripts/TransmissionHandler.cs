/*
    TransmissionHandler.cs
    - Moves the waves
    - Switches waves
    - Updates frequency text
    Contributor(s): Jake Schott
    Last Updated: 9/1/2025
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TransmissionHandler : NetworkBehaviour, IPowerable
{
    //CLASS CONSTANTS
    private static float WAVE_SPEED = 0.05f;
    private static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles

    public Material unlit_neon;
    public Material lit_neon;
    public Material unlit_green;
    public Material lit_green;
    public Material unlit_red;
    public Material lit_red;

    public GameObject frequency_display;
    public GameObject wave_display;
    public List<GameObject> waves = null;
    public GameObject progress_lights;
    public GameObject msg_preview_display;
    public GameObject success_indicator;
    public GameObject failure_indicator;

    private List<string> frequencies = new List<string>() { "120.5", "126.1", "129.4", "129.8", "134.3", "139.9" };
    private List<int> corresponding_waves = new List<int>() { 0, 0, 0, 0, 1, 0 };

    private bool is_powered = false;
    private List<int> transmission_code_indexes = null;
    private List<int> transmission_colors = null;
    private List<int> transmission_is_numeric = null;
    private int frequency_index = 0;
    private float shift = 0.0f;
    private Coroutine signal_transmission_coroutine = null;

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

    private void displayAdjustment()
    {
        frequency_display.transform.GetChild(0).GetComponent<TMP_Text>().SetText(frequencies[frequency_index] + "MH");
        for (int i = 0; i < waves.Count; i++)
        {
            waves[i].GetComponent<UnityEngine.UI.RawImage>().texture = wave_display.transform.GetChild(3).GetChild(corresponding_waves[frequency_index]).gameObject.GetComponent<UnityEngine.UI.RawImage>().mainTexture;
        }
    }

    public bool isTransmitting()
    {
        return (signal_transmission_coroutine != null);
    }

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
        displayAdjustment();
    }

    public int getCurrentFrequencyIndex()
    {
        return frequency_index;
    }

    public bool getIsPowered()
    {
        return is_powered;
    }

    private void resetProgressLights()
    {
        for (int i = 0; i < progress_lights.transform.childCount; i++)
        {
            progress_lights.transform.GetChild(i).GetComponent<Renderer>().material = unlit_neon;
        }
    }

    IEnumerator signalTransmission(int index)
    {
        bool successful_transmission = false;
        for (int k = 0; k < 8; k++)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int x = i * 4; x < (i * 4) + 4; x++)
                {
                    if (Random.Range(0, 2) == 0)
                    {
                        progress_lights.transform.GetChild(x).GetComponent<Renderer>().material = lit_neon;
                    }
                }
                yield return new WaitForSeconds(0.08f);
                resetProgressLights();
            }
            if (index == 1) //broadcast, remove circles
            {
                msg_preview_display.transform.GetChild(7 - k).gameObject.SetActive(false);
            }
            else //receive, add circles
            {
                //msg_preview_display.transform.GetChild(1 + k).gameObject.SetActive(true);
            }
        }

        if (index == 1) //broadcast
        {
            IUniversalCommunicable transmission_receiver = findReceiver();
            if (transmission_receiver != null)
            {
                successful_transmission = transmission_receiver.checkTransmission(frequency_index, transmission_code_indexes, transmission_colors, transmission_is_numeric);
                transmission_receiver.handleTransmission(frequency_index, transmission_code_indexes, transmission_colors, transmission_is_numeric);
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

        if (successful_transmission == true)
        {
            success_indicator.GetComponent<Renderer>().material = lit_green;
        }
        else
        {
            failure_indicator.GetComponent<Renderer>().material = lit_red;
        }

        yield return new WaitForSeconds(1.0f);

        transform.GetComponent<PowerControl>().power_manager.controlPowerChange(1, this.GetType().Name, 0.0f);

        success_indicator.GetComponent<Renderer>().material = unlit_green;
        failure_indicator.GetComponent<Renderer>().material = unlit_red;

        gameObject.GetComponent<InputOutputToggle>().activate(); 
        gameObject.GetComponent<SignalOptions>().activate();
        gameObject.GetComponent<FrequencyAdjuster>().activate();
        if (gameObject.GetComponent<InputOutputToggle>().getIsInputMode() == true)
        {
            gameObject.GetComponent<CharacterInput>().activate();
            gameObject.GetComponent<ResetDisplay>().activate();
        }

        signal_transmission_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        frequency_display.SetActive(true);
        wave_display.SetActive(true);
        msg_preview_display.SetActive(true);
        transform.GetComponent<SignalOptions>().activate();
        transform.GetComponent<FrequencyAdjuster>().activate();
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        frequency_display.SetActive(false);
        wave_display.SetActive(false);
        msg_preview_display.SetActive(false);
        transform.GetComponent<SignalOptions>().deactivate();
        transform.GetComponent<FrequencyAdjuster>().deactivate();

        if (signal_transmission_coroutine != null)
        {
            StopCoroutine(signal_transmission_coroutine);
            signal_transmission_coroutine = null;
            resetProgressLights();
            success_indicator.GetComponent<Renderer>().material = unlit_green;
            failure_indicator.GetComponent<Renderer>().material = unlit_red;
        }
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
        shift += dt * WAVE_SPEED;
        if (shift > 0.08f)
        {
            shift -= 0.08f;
        }
        for (int i = 0; i < waves.Count; i++)
        {
            waves[i].transform.localPosition = new Vector3(0f, -0.04f + (0.08f * i) - shift, 0f);
        }
    }

    //called by SignalOptions
    public void transmitSignal(int index) //0 is receive, 1 is broadcast
    {
        if (index == 0) //receive
        {
            transmitSignalTransmissionRPC(index, frequency_index, "", "", "");
        }
        else //broadcast
        {
            UniversalCommunicator uc = gameObject.GetComponent<UniversalCommunicator>();
            transmitSignalTransmissionRPC(index,
                                          frequency_index,
                                          DataConverter.listToString(uc.getCodeIndexes()),
                                          DataConverter.listToString(uc.getCodeColors()),
                                          DataConverter.listToString(uc.getCodeIsNumeric()));
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSignalTransmissionRPC(int index, int freq, string s_code_indexes, string s_colors, string s_is_numeric)
    {
        gameObject.GetComponent<SignalOptions>().deactivate();
        gameObject.GetComponent<FrequencyAdjuster>().deactivate();
        gameObject.GetComponent<SignalOptions>().returnDials();

        if (index == 1) //broadcast
        {
            transmission_code_indexes = DataConverter.stringToList(s_code_indexes);
            transmission_colors = DataConverter.stringToList(s_colors);
            transmission_is_numeric = DataConverter.stringToList(s_is_numeric);
        }

        UniversalCommunicator uc = gameObject.GetComponent<UniversalCommunicator>();
        uc.clearUC();
        if (index == 0)
        {
            uc.clearMsgPreview();
        }
        InputOutputToggle iot = gameObject.GetComponent<InputOutputToggle>();
        if (iot.getIsInputMode() == true && index == 0)
        {
            iot.forceSwitch(false);
        }
        else
        {
            gameObject.GetComponent<CharacterInput>().deactivate();
            gameObject.GetComponent<ResetDisplay>().deactivate();
        }
        iot.deactivate();

        transform.GetComponent<PowerControl>().power_manager.controlPowerChange(1, this.GetType().Name, MAX_POWER_CONSUMPTION);

        if (signal_transmission_coroutine != null)
        {
            StopCoroutine(signal_transmission_coroutine);
        }
        signal_transmission_coroutine = StartCoroutine(signalTransmission(index));
    }
}
