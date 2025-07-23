/*
    SignalOptions.cs
    - Sends or receives a transmission
    Contributor(s): Jake Schott
    Last Updated: 5/17/2025
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SignalOptions : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float PUSH_TIME = 0.5f;
    private static int FLASHES = 20;
    private static float FLASH_DELAY = 0.1f;
    private static float GREEN_DELAY = 1.0f;

    private string CONTROL_NAME = "SIGNAL OPTIONS";
    private List<string> CONTROL_DESCS = new List<string> { "RECEIVE", "BROADCAST" };
    private List<int> CONTROL_INDEXES = new List<int>() { 2, 0 };
    private List<Button> BUTTONS = new List<Button>();

    public List<GameObject> signal_buttons = null;
    public List<GameObject> signal_list = null;
    public GameObject success_light;
    public GameObject fail_light;

    public Material unlit_blue;
    public Material neon;
    public Material lit_red;
    public Material unlit_red;
    public Material lit_green;
    public Material unlit_green;

    private Vector3[] initial_pos = new Vector3[2];
    private Vector3 push_direction = new Vector3(0.0019f, -0.0053f, 0f);

    private Coroutine signal_transmission_coroutine = null;
    private Coroutine button_push_coroutine = null;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, true));
        hud_info.setButtons(BUTTONS);

        //set initial positions
        for (int i = 0; i <= 1; i++)
        {
            initial_pos[i] = signal_buttons[i].transform.localPosition;
        }
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    IEnumerator buttonPush(int index)
    {
        //set buttons to initial positions
        for (int i = 0; i <= 1; i++)
        {
            signal_buttons[i].transform.localPosition = initial_pos[i];
        }

        Vector3 final_pos = initial_pos[index] + push_direction;

        for (int i = 0; i <= 1; i++)
        {
            float half_time = PUSH_TIME * 0.5f;
            float push_time = half_time;

            while (push_time > 0.0f)
            {
                float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
                push_time = Mathf.Max(0.0f, push_time - dt);

                float push_percentage = 1.0f - (push_time / half_time);
                if (i == 1)
                {
                    push_percentage = (push_time / half_time);
                }

                signal_buttons[index].transform.localPosition =
                    new Vector3(Mathf.Lerp(initial_pos[i].x, final_pos.x, push_percentage),
                                Mathf.Lerp(initial_pos[i].y, final_pos.y, push_percentage),
                                Mathf.Lerp(initial_pos[i].z, final_pos.z, push_percentage));

                yield return null;
            }
        }

        button_push_coroutine = null;
    }

    IEnumerator signalTransmission(int index)
    {
        if (button_push_coroutine != null)
        {
            StopCoroutine(button_push_coroutine);
        }
        button_push_coroutine = StartCoroutine(buttonPush(index));

        //wait for button to pushed
        while (button_push_coroutine != null)
        {
            yield return null;
        }

        for (int i = 0; i < FLASHES; i++)
        {
            for (int x = 0; x < signal_list.Count; x++)
            {
                if (UnityEngine.Random.Range(0,3) != 0)
                {
                    signal_list[x].GetComponent<Renderer>().material = unlit_blue;
                }
                else
                {
                    signal_list[x].GetComponent<Renderer>().material = neon;
                }
            }
            yield return new WaitForSeconds(FLASH_DELAY);
        }

        for (int x = 0; x < signal_list.Count; x++)
        {
            signal_list[x].GetComponent<Renderer>().material = unlit_blue;
        }

        success_light.GetComponent<Renderer>().material = lit_green;

        yield return new WaitForSeconds(GREEN_DELAY);

        success_light.GetComponent<Renderer>().material = unlit_green;

        BUTTONS[0].updateInteractable(true);
        BUTTONS[1].updateInteractable(true);

        signal_transmission_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        //make sure transmit not active
        if (signal_transmission_coroutine == null)
        {
            for (int i = 0; i < CONTROL_INDEXES.Count; i++)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    BUTTONS[i].toggle(0.2f);
                    BUTTONS[i % 1].updateInteractable(false);
                    transmitSignalTransmissionRPC(i);
                    return;
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSignalTransmissionRPC(int index)
    {
        UniversalCommunicator uc = gameObject.GetComponent<UniversalCommunicator>();
        GameObject scenario_handler = GameObject.FindGameObjectWithTag("ScenarioHandler");
        if (scenario_handler != null)
        {
            Component[] scenario_handler_components = scenario_handler.GetComponents<Component>();
            for (int i = 0; i < scenario_handler_components.Length; i++)
            {
                IUniversalCommunicable transmission_receiver = scenario_handler_components[i] as IUniversalCommunicable;
                if (transmission_receiver != null)
                {
                    transmission_receiver.handleTransmission(uc.getCodeIndexes(), uc.getCodeColors(), uc.getCodeIsNumeric());
                    break;
                }
            }
        }
        uc.resetDisplay();

        if (signal_transmission_coroutine != null)
        {
            StopCoroutine(signal_transmission_coroutine);
        }
        signal_transmission_coroutine = StartCoroutine(signalTransmission(index));
    }
}
