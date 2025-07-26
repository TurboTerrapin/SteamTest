/*
    ProbeOptions.cs
    - Handles launching of probe
    - Handles destroying of probe
    Contributor(s): Jake Schott
    Last Updated: 7/25/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ProbeOptions : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 1.5f;
    private static float CHARGE_TIME = 3.0f;
    private static float FUNCTION_TIME = 4.0f;

    private string[] CONTROL_NAMES = new string[2] { "LAUNCH PROBE", "DESTROY PROBE" };
    private List<string> CONTROL_DESCS = new List<string> { "ACTIVATE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public List<GameObject> dials = null;
    public GameObject probe_options_canvas;
    public GameObject probe_feed_canvas;
    public GameObject probe_range_canvas;
    public GameObject probe_health_canvas;
    public GameObject probe_prefab;

    private bool[] active_dials = new bool[2] { true, false };
    private Coroutine dial_turn_coroutine = null;
    private Coroutine dial_activation_coroutine = null;
    private float dial_turn_percentage = 0.0f;
    private float function_charge_percentage = 0.0f;
    private GameObject current_probe = null;
    private TacticianProbeInfo tactician_probe_info = null;

    private List<KeyCode> keys_down = new List<KeyCode>();
    private List<string> ray_targets = new List<string> { "probe_launch_dial", "probe_destruct_dial" };
    private int ray_target_index = -1;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        clearAllProbes();

        tactician_probe_info = GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<TacticianProbeInfo>();

        hud_info = new HUDInfo(CONTROL_NAMES[0]);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, false));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));

        hud_info.setButtons(BUTTON_LISTS[0]);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);

        hud_info.setButtons(BUTTON_LISTS[index]);
        return hud_info;
    }

    //used to ensure there is only one probe at a time
    private void clearAllProbes()
    {
        foreach (GameObject probe in GameObject.FindGameObjectsWithTag("Probe"))
        {
            GameObject.Destroy(probe);
        }
    }

    //called any time a probe is destroyed
    public void onProbeDestroyed()
    {
        if (dial_activation_coroutine == null)
        {
            //turning to destroy
            if (dial_turn_coroutine != null)
            {
                StopCoroutine(dial_turn_coroutine);
                dial_turn_coroutine = StartCoroutine(dialReturn(1, dial_turn_percentage));
            }
            active_dials[0] = true;
            active_dials[1] = false;
            BUTTON_LISTS[0][0].updateInteractable(true);
            BUTTON_LISTS[1][0].updateInteractable(false);
        }
    }

    //spawns a probe, links probe to probe controls
    private void spawnProbe()
    {
        current_probe = GameObject.Instantiate(probe_prefab, GameObject.FindGameObjectWithTag("WorldRoot").transform);
        current_probe.transform.position = new Vector3(0, -12.5f, 0);
        current_probe.transform.rotation = GameObject.FindGameObjectWithTag("Spaceship").transform.rotation;
        transform.GetComponent<ProbeLateralMovement>().linkProbe(current_probe);
        transform.GetComponent<ProbeVerticalMovement>().linkProbe(current_probe);
        transform.GetComponent<ProbeOrientation>().linkProbe(current_probe);
    }

    private void setChargeColor(Color new_color)
    {
        for (int i = 1; i <= 3; i++)
        {
            probe_options_canvas.transform.GetChild(i).GetComponent<UnityEngine.UI.Image>().color = new_color;
        }
    }

    //turns corresponding dial based on dial_turn_percentage
    private void displayDialAdjustment(int index)
    {
        //turn corresponding dial
        dials[index].transform.localRotation =
            Quaternion.Euler(dials[index].transform.localEulerAngles.x,
                             dials[index].transform.localEulerAngles.y,
                             Mathf.Lerp(90.0f, 180.0f, dial_turn_percentage));
    }

    //adjusts the fill bar and colors
    private void displayScreenAdjustment(int index)
    {
        //adjust fill bar
        if (index == 0)
        {
            setChargeColor(new Color(0.0f, 0.84f, 1.0f, 1.0f));
            probe_options_canvas.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().fillAmount = function_charge_percentage;
        }
        else
        {
            if (function_charge_percentage > 0.0f)
            {
                setChargeColor(new Color(1.0f, 0.0f, 0.0f, 1.0f));
            }
            else
            {
                setChargeColor(new Color(0.0f, 0.84f, 1.0f, 1.0f));
            }
            probe_options_canvas.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().fillAmount = 1.0f - function_charge_percentage;
        }
    }

    //enable destruct button
    public void linkProbe()
    {
        BUTTON_LISTS[1][0].updateInteractable(true);
    }

    //disable destruct button
    public void unlinkProbe()
    {
        BUTTON_LISTS[1][0].updateInteractable(false);
    }

    private bool checkNeutralState()
    {
        return (dial_turn_percentage <= 0.0f && function_charge_percentage <= 0.0f);
    }

    //initial charging
    IEnumerator dialTurn(int index)
    {
        while (keys_down.Count > 0 || checkNeutralState() == false)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            bool currently_active = true;
            if (index == 1)
            {
                if (current_probe != null)
                {
                    currently_active = (current_probe.GetComponent<Probe>().inRange());
                }
            }

            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && ray_target_index == index && currently_active == true)
            {
                dial_turn_percentage = Mathf.Min(1.0f, dial_turn_percentage + (dt / TURN_TIME));
                if (dial_turn_percentage >= 1.0f)
                {
                    function_charge_percentage = Mathf.Min(1.0f, function_charge_percentage + (dt / CHARGE_TIME));
                }
                if (function_charge_percentage >= 1.0f)
                {
                    transmitFunctionActivationRPC(index);
                }
            }
            else
            {
                dial_turn_percentage = Mathf.Max(0.0f, dial_turn_percentage - (dt / TURN_TIME));
                function_charge_percentage = Mathf.Max(0.0f, function_charge_percentage - (dt / CHARGE_TIME));
            }

            transmitDialTurnRPC(index, dial_turn_percentage, function_charge_percentage);

            keys_down.Clear();
            ray_target_index = -1;
            yield return null;
        }

        dial_turn_coroutine = null;
    }

    //used after charge complete
    IEnumerator dialReturn(int index, float starting_percentage)
    {
        float anim_time = (starting_percentage * TURN_TIME / 2.0f);
        while (anim_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            anim_time = Mathf.Max(0.0f, anim_time - dt);
            dial_turn_percentage = anim_time / (TURN_TIME / 2.0f);
            displayDialAdjustment(index);
            yield return null;
        }

        dial_turn_coroutine = null;
    }

    //charge complete, either launch probe or destroy it
    IEnumerator activateFunction(int index)
    {
        dial_turn_coroutine = StartCoroutine(dialReturn(index, 1.0f));

        if (index == 0) //launch probe
        {
            probe_health_canvas.transform.GetChild(1).gameObject.SetActive(true);
            probe_feed_canvas.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
            float anim_time = FUNCTION_TIME;
            while (anim_time > 0.0f)
            {
                float dt = Time.deltaTime;
                anim_time = Mathf.Max(0.0f, anim_time - dt);

                //rotate probe icon on probe options screen
                probe_options_canvas.transform.GetChild(1).localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f * (anim_time % 1.0f));

                //highlight, rotate, and shift up probe icon on probe feed screen
                probe_feed_canvas.transform.GetChild(3).localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f * (anim_time % 1.0f));
                probe_feed_canvas.transform.GetChild(3).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, Mathf.Lerp(1.0f, 0.196f, anim_time / FUNCTION_TIME));
                probe_feed_canvas.transform.GetChild(3).localPosition = new Vector3(0.0f, Mathf.Lerp(0.02f, 0.0f, Mathf.Max(0.0f, anim_time - FUNCTION_TIME - 1.0f) / 1.0f), 0.0f);

                //move probe feed progress bar
                probe_feed_canvas.transform.GetChild(4).gameObject.SetActive(anim_time < (FUNCTION_TIME - 1.0f));
                probe_feed_canvas.transform.GetChild(4).GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = 1.0f - (anim_time / (FUNCTION_TIME - 1.0f));

                //highlight the different scan waves for the probe distance screen
                tactician_probe_info.displayRange(1.0f - (anim_time / FUNCTION_TIME));

                //increase probe health and highlight the border
                tactician_probe_info.displayHealth(100.0f - (100.0f * anim_time / FUNCTION_TIME));
                probe_health_canvas.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, Mathf.Lerp(1.0f, 0.196f, anim_time / FUNCTION_TIME));

                yield return null;
            }
            probe_range_canvas.transform.GetChild(12).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);

            yield return new WaitForSeconds(0.5f);

            clearAllProbes();
            spawnProbe();

            probe_feed_canvas.transform.GetChild(1).gameObject.SetActive(true);
            probe_feed_canvas.transform.GetChild(2).gameObject.SetActive(true);
            probe_feed_canvas.transform.GetChild(3).gameObject.SetActive(false);
            probe_feed_canvas.transform.GetChild(4).gameObject.SetActive(false);
        }
        else //destroy probe
        {
            yield return new WaitForSeconds(0.5f);
            if (current_probe != null)
            {
                //kills probe
                current_probe.GetComponent<Probe>().damageProbe(150.0f);
            }
            setChargeColor(new Color(0.0f, 0.84f, 1.0f, 1.0f));
            yield return new WaitForSeconds(1.5f);
            probe_feed_canvas.transform.GetChild(1).gameObject.SetActive(false);
            probe_feed_canvas.transform.GetChild(2).gameObject.SetActive(false);
            probe_feed_canvas.transform.GetChild(3).transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            probe_feed_canvas.transform.GetChild(3).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.196f);
            probe_feed_canvas.transform.GetChild(3).gameObject.SetActive(true);
            probe_feed_canvas.transform.GetChild(4).gameObject.SetActive(false);
            probe_feed_canvas.transform.GetChild(5).gameObject.SetActive(false);
        }

        for (int i = 0; i <= 1; i++)
        {
            active_dials[i] = !active_dials[i];
            BUTTON_LISTS[i][0].updateInteractable(active_dials[i]);
        }

        if (dial_turn_coroutine != null)
        {
            StopCoroutine(dial_turn_coroutine);
            dial_turn_coroutine = null;
        }

        dial_turn_percentage = 0.0f;
        function_charge_percentage = 0.0f;
        dial_activation_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;
        ray_target_index = ray_targets.IndexOf(current_target.name);

        if (dial_turn_percentage == 0.0f && active_dials[ray_target_index] == true && dial_activation_coroutine == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                if (dial_turn_coroutine == null)
                {
                    dial_turn_coroutine = StartCoroutine(dialTurn(ray_target_index));
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDialTurnRPC(int index, float dial_percent, float function_percent)
    {
        dial_turn_percentage = dial_percent;
        function_charge_percentage = function_percent;
        displayDialAdjustment(index);
        displayScreenAdjustment(index);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitFunctionActivationRPC(int index)
    {
        BUTTON_LISTS[index][0].updateInteractable(false);
        if (dial_turn_coroutine != null)
        {
            StopCoroutine(dial_turn_coroutine);
        }
        dial_turn_coroutine = null;

        dial_activation_coroutine = StartCoroutine(activateFunction(index));
    }
}
