/*
    LifeformScanner.cs
    - Handles scanning for life-forms on the ship and outside the ship
    Contributor(s): Jake Schott
    Last Updated: 8/27/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.ComponentModel.Design;

//contains info for a lifeform scan
public struct LifeformScanData
{
    public string lifeform_name;
    public int lifeform_count;
    public string lifeform_DNA_sequence;
    public int lifeform_hostility;

    public LifeformScanData(string name, int count, string sequence, int hostility)
    {
        this.lifeform_name = name;
        this.lifeform_count = count;
        this.lifeform_DNA_sequence = sequence;
        this.lifeform_hostility = hostility;
    }
}

public class LifeformScanner : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float MAX_POWER_CONSUMPTION = 0.3f; //equates to 3 circles
    private static Vector3 BUTTON_PUSH_DIRECTION = new Vector3(0.0f, -0.005f, 0.002f);
    private static float BUTTON_PUSH_TIME = 0.2f;
    private static Vector3 SWITCH_MOVEMENT_DIRECTION = new Vector3(0.021f, 0.0f, 0.0f);
    private static float SWITCH_TIME = 0.5f;
    private static float[] SCAN_SPIN_SPEEDS = new float[] { 150.0f, 250.0f };
    private static float SCAN_TIME = 4.0f;
    private static float[] SCAN_ARROW_DIRECTIONS = new float[] { 1.0f, -1.0f };
    private static string[] SCAN_OPTIONS = new string[] { "INTERIOR", "EXTERIOR" };
    private static string[] HOSTILITY_OPTIONS = new string[] { "FRIENDLY", "NEUTRAL", "HOSTILE" };

    private string CONTROL_NAME = "LIFE-FORM SCANNER";
    private List<string> INFO_MESSAGES = new List<string>() { "Selects whether to scan inside of ship or outside ship.", "" };
    private List<string> CONTROL_DESCS = new List<string> { "SWITCH", "SCAN" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6, 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public GameObject lifeform_scanner_display;
    public GameObject lifeform_scanner_switch;
    public GameObject lifeform_scanner_button;

    private bool is_powered = false;
    private int current_state = 0;
    private Coroutine scan_mode_switch_coroutine = null;
    private Coroutine scan_animator_coroutine = null;
    private Coroutine scan_coroutine = null;

    private List<string> ray_targets = new List<string> { "lifeform_scanner_switch", "lifeform_scanner_button" };

    private static HUDInfo[] hud_infos = new HUDInfo[2];

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public int finger_position = 0;
    public Vector3 right_hand_offset = Vector3.zero;
    public float lerp_speed = 5f;

    private void Start()
    {
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));

        for (int i = 0; i < 2; i++)
        {
            hud_infos[i] = new HUDInfo(CONTROL_NAME);
            hud_infos[i].setButtons(BUTTON_LISTS[i]);
            hud_infos[i].setInfo(INFO_MESSAGES[i]);
        }
        hud_infos[1].setMaxPowerConsumption(MAX_POWER_CONSUMPTION);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_infos[ray_targets.IndexOf(current_target.name)];
    }

    public Transform getIKTarget(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        return IK_targets[index].transform;
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

    private void updatePowerConsumption(float consumption)
    {
        ReferenceAssistor.Instance.power_manager.controlPowerChange(1, this.GetType().Name, consumption);
        hud_infos[1].setPowerConsumption(consumption);
    }

    //resets lifeform scanner to default state
    public void resetToDefault()
    {
        resetCoroutines();
        current_state = 0;
        displayStateAdjustment();
        displayScanViewContents(0);
        setView(0);
        lifeform_scanner_switch.transform.localPosition = Vector3.zero;
    }

    private void setView(int index)
    {
        for (int i = 0; i < 2; i++)
        {
            lifeform_scanner_display.transform.GetChild(i).gameObject.SetActive(i == index);
        }
    }
    
    private void displayStickLitIndicatorAdjustment()
    {
        if (is_powered == true)
        {
            if (current_state == 0)
            {
                lifeform_scanner_switch.transform.GetChild(0).GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.lit_neon;
            }
            else
            {
                lifeform_scanner_switch.transform.GetChild(0).GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.lit_purple;
            }
        }
        else
        {
            lifeform_scanner_switch.transform.GetChild(0).GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.pure_black;
        }
    }

    private void displayStateAdjustment()
    {
        //update stick
        displayStickLitIndicatorAdjustment();

        //update UI
        ManualColorSwitcher.changeColor(lifeform_scanner_display.transform.GetChild(0).gameObject, ReferenceAssistor.COLOR_OPTIONS[current_state]);
        ManualColorSwitcher.changeColor(lifeform_scanner_display.transform.GetChild(1).GetChild(0).gameObject, ReferenceAssistor.COLOR_OPTIONS[current_state]);
        lifeform_scanner_display.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<TMP_Text>().SetText(SCAN_OPTIONS[current_state]);
        for (int i = 0; i < 2; i++)
        {
            lifeform_scanner_display.transform.GetChild(0).GetChild(1).GetChild(i + 1).localScale = new Vector3(SCAN_ARROW_DIRECTIONS[current_state], 1.0f, 1.0f);
        }
    }

    private void displayScanViewContents(int index)
    {
        for (int i = 0; i < 3; i++)
        {
            lifeform_scanner_display.transform.GetChild(0).GetChild(i + 1).gameObject.SetActive(i == index);
        }
    }

    private void displayProgressCircleFillAmount(float amount)
    {
        lifeform_scanner_display.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = amount;
    }

    private ILifeformCommunicable getLifeformTracker()
    {
        GameObject scenario_handler = GameObject.FindGameObjectWithTag("ScenarioHandler");
        if (scenario_handler != null && scenario_handler.GetComponent<ILifeformCommunicable>() != null)
        {
            return scenario_handler.GetComponent<ILifeformCommunicable>();
        }
        return null;
    }

    private void displayLSD(LifeformScanData lsd)
    {
        lifeform_scanner_display.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<TMP_Text>().SetText("\"" + lsd.lifeform_name + "\"");
        lifeform_scanner_display.transform.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetComponent<TMP_Text>().SetText(lsd.lifeform_count.ToString());
        lifeform_scanner_display.transform.GetChild(1).GetChild(0).GetChild(2).GetChild(0).GetComponent<TMP_Text>().SetText(lsd.lifeform_DNA_sequence);
        for (int i = 0; i < 3; i++)
        {
            lifeform_scanner_display.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(i).gameObject.SetActive(i == lsd.lifeform_hostility);
        }
        lifeform_scanner_display.transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<TMP_Text>().color = 
            lifeform_scanner_display.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(lsd.lifeform_hostility).GetComponent<UnityEngine.UI.RawImage>().color;
        lifeform_scanner_display.transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<TMP_Text>().SetText(HOSTILITY_OPTIONS[lsd.lifeform_hostility]);
    }

    IEnumerator lifeformScannerStateSwitch(int state_to_switch_to)
    {
        Vector3 start_pos = lifeform_scanner_switch.transform.localPosition;
        Vector3 end_pos = SWITCH_MOVEMENT_DIRECTION * state_to_switch_to;

        //move switch
        float anim_time = SWITCH_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            lifeform_scanner_switch.transform.localPosition = Vector3.Lerp(end_pos, start_pos, anim_time / SWITCH_TIME);

            yield return null;
        }

        current_state = state_to_switch_to;
        displayStateAdjustment();
        setView(0);

        updateButtons(is_powered);
        resetCoroutines();
    }

    IEnumerator lifeformScanAnimator()
    {
        while (true)
        {
            foreach (Transform t in lifeform_scanner_display.transform.GetChild(0).GetChild(2).GetChild(1))
            {
                float a = 1.0f;
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    a = 0.08f;
                }
                t.GetComponent<CanvasGroup>().alpha = a;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator lifeformScannerScanSequence()
    {
        //push button in initially
        float anim_time = BUTTON_PUSH_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            lifeform_scanner_button.transform.localPosition = Vector3.Lerp(BUTTON_PUSH_DIRECTION, Vector3.zero, anim_time);

            yield return null;
        }

        updatePowerConsumption(MAX_POWER_CONSUMPTION);
        displayScanViewContents(1);
        setView(0);

        //do scanning animation and fill progress bar
        scan_animator_coroutine = StartCoroutine(lifeformScanAnimator());
        anim_time = SCAN_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            displayProgressCircleFillAmount(1.0f - (anim_time / SCAN_TIME));
            for (int i = 0; i < 2; i++)
            {
                float z = lifeform_scanner_display.transform.GetChild(0).GetChild(2).GetChild(i).transform.localEulerAngles.z + SCAN_SPIN_SPEEDS[i] * Time.deltaTime;
                lifeform_scanner_display.transform.GetChild(0).GetChild(2).GetChild(i).transform.localRotation = Quaternion.Euler(0.0f, 0.0f, z);
            }

            yield return null;
        }
        StopCoroutine(scan_animator_coroutine);

        //display checkmark if successful, cross if not
        ILifeformCommunicable ilc = getLifeformTracker();
        bool successful_scan = (ilc != null && ilc.hasLifeforms(current_state));
        if (successful_scan == true)
        {
            displayLSD(ilc.retrieveLifeformData(current_state));
        }
        lifeform_scanner_display.transform.GetChild(0).GetChild(3).GetChild(0).gameObject.SetActive(successful_scan);
        lifeform_scanner_display.transform.GetChild(0).GetChild(3).GetChild(1).gameObject.SetActive(!successful_scan);
        displayScanViewContents(2);

        //wait one second
        yield return new WaitForSeconds(1.0f);

        //pull button out
        anim_time = BUTTON_PUSH_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            lifeform_scanner_button.transform.localPosition = Vector3.Lerp(Vector3.zero, BUTTON_PUSH_DIRECTION, anim_time);

            yield return null;
        }

        //return to default view if unsuccessful or show results if successful
        displayProgressCircleFillAmount(0.0f);
        displayScanViewContents(0);
        if (successful_scan == false)
        {
            setView(0);
        }
        else
        {
            setView(1);
        }

        updatePowerConsumption(0.0f);
        updateButtons(is_powered);
        resetCoroutines();
    }

    IEnumerator returnToZero()
    {
        Vector3 starting_pos = lifeform_scanner_button.transform.localPosition;

        float anim_time = BUTTON_PUSH_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            lifeform_scanner_button.transform.localPosition = Vector3.Lerp(Vector3.zero, starting_pos, anim_time / BUTTON_PUSH_TIME);

            yield return null;
        }
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        if (scan_mode_switch_coroutine != null || scan_coroutine != null)
        {
            return;
        }

        int ray_target_index = ray_targets.IndexOf(current_target.name);
        if (ray_target_index == 0) //switch state
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                transmitLifeformScanSwitchRPC(1 - current_state);
            }
        } 
        else if (ray_target_index == 1) //initiate scan
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs))
            {
                transmitLifeformScanSequenceRPC();
            }
        }
    }

    private void updateButtons(bool active)
    {
        BUTTON_LISTS[0][0].updateInteractable(active);
        BUTTON_LISTS[1][0].updateInteractable(active);
    }

    private void resetCoroutines()
    {
        if (scan_mode_switch_coroutine != null)
        {
            StopCoroutine(scan_mode_switch_coroutine);
            scan_mode_switch_coroutine = null;
        }
        if (scan_coroutine != null)
        {
            StopCoroutine(scan_coroutine);
            scan_coroutine = null;
        }
        if (scan_animator_coroutine != null)
        {
            StopCoroutine(scan_animator_coroutine);
            scan_animator_coroutine = null;
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        lifeform_scanner_display.SetActive(true);
        updateButtons(true);
        displayStickLitIndicatorAdjustment();
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        lifeform_scanner_display.SetActive(false);
        updateButtons(false);
        displayStickLitIndicatorAdjustment();
        displayProgressCircleFillAmount(0.0f);
        displayScanViewContents(0);
        hud_infos[1].setPowerConsumption(0.0f);

        if (scan_coroutine != null)
        {
            StopCoroutine(scan_coroutine);
            scan_coroutine = null;
            if (scan_animator_coroutine != null)
            {
                StopCoroutine(scan_animator_coroutine);
                scan_animator_coroutine = null;
            }
            StartCoroutine(returnToZero());
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitLifeformScanSwitchRPC(int new_state)
    {
        updateButtons(false);
        resetCoroutines();
        scan_mode_switch_coroutine = StartCoroutine(lifeformScannerStateSwitch(new_state));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitLifeformScanSequenceRPC()
    {
        updateButtons(false);
        resetCoroutines();
        scan_coroutine = StartCoroutine(lifeformScannerScanSequence());
    }
}