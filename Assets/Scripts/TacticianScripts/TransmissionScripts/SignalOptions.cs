/*
    SignalOptions.cs
    - Sends or receives a transmission
    Contributor(s): Jake Schott
    Last Updated: 7/30/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SignalOptions : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 0.75f;

    private string CONTROL_NAME = "SIGNAL OPTIONS";
    private List<string> CONTROL_DESCS = new List<string>{"RECEIVE", "BROADCAST"};
    private List<int> CONTROL_INDEXES = new List<int>(){6};
    private List<Button>[] BUTTON_LISTS = new List<Button>[2]{new List<Button>(), new List<Button>()};

    public Material unlit_blue;
    public Material neon;
    public Material unlit_green;
    public Material lit_green;
    public Material unlit_red;
    public Material lit_red;

    public List<GameObject> dials = null;
    public GameObject progress_lights;
    public GameObject msg_preview_display;
    public GameObject success_indicator;
    public GameObject failure_indicator;

    private List<int> transmission_code_indexes = null;
    private List<int> transmission_colors = null;
    private List<int> transmission_is_numeric = null;
    private Coroutine dial_turn_coroutine = null;
    private Coroutine signal_transmission_coroutine = null;
    private float[] dial_turn_percentages = { 0.0f, 0.0f };

    private List<KeyCode> keys_down = new List<KeyCode>();
    private List<string> ray_targets = new List<string> { "transmission_receive", "transmission_broadcast" };
    private int ray_target_index = -1;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, false));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[0], true, false));

        hud_info.setButtons(BUTTON_LISTS[0]);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setButtons(BUTTON_LISTS[index]);
        return hud_info;
    }

    private void displayDialTurn(int index)
    {
        dials[index].transform.localRotation =
            Quaternion.Euler(dials[index].transform.localEulerAngles.x,
                             dials[index].transform.localEulerAngles.y,
                             Mathf.Lerp(180.0f, 90.0f, dial_turn_percentages[index]));
    }

    private bool checkNeutralState()
    {
        for (int i = 0; i < 2; i++)
        {
            if (dial_turn_percentages[i] > 0.0f && signal_transmission_coroutine == null)
            {
                return false;
            }
        }
        return true;
    }

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

    IEnumerator dialReturn()
    {
        while (dial_turn_percentages[0] > 0.0f || dial_turn_percentages[1] > 0.0f)
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < 2; i++)
            {
                dial_turn_percentages[i] = Mathf.Max(0.0f, dial_turn_percentages[i] - (dt / TURN_TIME));
                displayDialTurn(i);
            }
            yield return null;
        }

        dial_turn_coroutine = null;
    }

    IEnumerator dialTurn()
    {
        while (keys_down.Count > 0 || checkNeutralState() == false)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            if (ray_target_index >= 0)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down))
                {
                    dial_turn_percentages[ray_target_index] = Mathf.Min(1.0f, dial_turn_percentages[ray_target_index] + (dt / TURN_TIME));
                    if (dial_turn_percentages[ray_target_index] >= 1.0f)
                    {
                        BUTTON_LISTS[0][0].updateInteractable(false);
                        BUTTON_LISTS[1][0].updateInteractable(false);
                        int frequency = gameObject.GetComponent<TransmissionHandler>().getCurrentFrequencyIndex();
                        if (ray_target_index == 0) //receive
                        {
                            transmitSignalTransmissionRPC(ray_target_index, frequency, "", "", "");
                        }
                        else //broadcast
                        {
                            UniversalCommunicator uc = gameObject.GetComponent<UniversalCommunicator>();
                            transmitSignalTransmissionRPC(ray_target_index,
                                                          frequency,     
                                                          DataConverter.listToString(uc.getCodeIndexes()),
                                                          DataConverter.listToString(uc.getCodeColors()),
                                                          DataConverter.listToString(uc.getCodeIsNumeric()));
                        }
                    }
                }
                else
                {
                    dial_turn_percentages[ray_target_index] = Mathf.Max(0.0f, dial_turn_percentages[ray_target_index] - (dt / TURN_TIME));
                }
            }

            for (int i = 0; i < 2; i++)
            {
                if (i != ray_target_index)
                {
                    dial_turn_percentages[i] = Mathf.Max(0.0f, dial_turn_percentages[i] - (dt / TURN_TIME));
                }
            }

            transmitDialArmRPC(dial_turn_percentages[0], dial_turn_percentages[1]);

            keys_down.Clear();
            ray_target_index = -1;
            yield return null;
        }

        dial_turn_coroutine = null;
    }

    private void resetProgressLights()
    {
        for (int i = 0; i < progress_lights.transform.childCount; i++)
        {
            progress_lights.transform.GetChild(i).GetComponent<Renderer>().material = unlit_blue;
        }
    }

    IEnumerator signalTransmission(int index, int freq)
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
                        progress_lights.transform.GetChild(x).GetComponent<Renderer>().material = neon;
                    }
                }
                yield return new WaitForSeconds(0.08f);
                resetProgressLights();
            }
            if (index == 1) //broadcast, remove circles
            {
                msg_preview_display.transform.GetChild(1 + (7 - k)).gameObject.SetActive(false);
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
                successful_transmission = transmission_receiver.checkTransmission(freq, transmission_code_indexes, transmission_colors, transmission_is_numeric);
                transmission_receiver.handleTransmission(freq,transmission_code_indexes, transmission_colors, transmission_is_numeric);
            }
        }
        else //receive
        {

        }

        if (successful_transmission)
        {
            success_indicator.GetComponent<Renderer>().material = lit_green;
        }
        else
        {
            failure_indicator.GetComponent<Renderer>().material = lit_red;
        }

        yield return new WaitForSeconds(1.0f);

        success_indicator.GetComponent<Renderer>().material = unlit_green;
        failure_indicator.GetComponent<Renderer>().material = unlit_red;

        for (int i = 0; i < 2; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(true);
            dial_turn_percentages[i] = 0.0f;
        }

        gameObject.GetComponent<InputOutputToggle>().activate();
        if (index == 1)
        {
            gameObject.GetComponent<CharacterInput>().activate();
        }

        signal_transmission_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;
        ray_target_index = ray_targets.IndexOf(current_target.name);

        if (dial_turn_percentages[ray_target_index] == 0.0f && signal_transmission_coroutine == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                if (dial_turn_coroutine == null)
                {
                    dial_turn_coroutine = StartCoroutine(dialTurn());
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDialArmRPC(float dp_receive, float dp_broadcast)
    {
        dial_turn_percentages[0] = dp_receive;
        dial_turn_percentages[1] = dp_broadcast;

        for (int i = 0; i < 2; i++)
        {
            displayDialTurn(i);
        }
    }

    
    [Rpc(SendTo.Everyone)]
    private void transmitSignalTransmissionRPC(int index, int freq, string s_code_indexes, string s_colors, string s_is_numeric)
    {
        if (dial_turn_coroutine != null)
        {
            StopCoroutine(dial_turn_coroutine);
            dial_turn_coroutine = null;
        }
        dial_turn_coroutine = StartCoroutine(dialReturn());

        if (index == 1) //broadcast
        {
            transmission_code_indexes = DataConverter.stringToList(s_code_indexes);
            transmission_colors = DataConverter.stringToList(s_colors);
            transmission_is_numeric = DataConverter.stringToList(s_is_numeric);
        }

        UniversalCommunicator uc = gameObject.GetComponent<UniversalCommunicator>();
        uc.clearUC();
        InputOutputToggle iot = gameObject.GetComponent<InputOutputToggle>();
        if (iot.getIsInputMode() == true && index == 0)
        {
            iot.forceSwitch(false);
        }
        else
        {
            gameObject.GetComponent<CharacterInput>().deactivate();
        }
        iot.deactivate();

        if (signal_transmission_coroutine != null)
        {
            StopCoroutine(signal_transmission_coroutine);
        }
        signal_transmission_coroutine = StartCoroutine(signalTransmission(index, freq));
    }
}