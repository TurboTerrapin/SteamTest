/*
    ShipStatus.cs
    - Handles slider
    - Enables/disables red alert
    Contributor(s): Jake Schott
    Last Updated: 4/24/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ShipStatus: NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static Color[] COLOR_OPTIONS = new Color[3] { new Color(0.0f, 0.84f, 1.0f), new Color(1.0f, 0.47f, 0.0f), new Color(1.0f, 0.0f, 0.0f)};
    private static float MOVE_TIME = 0.5f;
    private static float MAX_POWER_CONSUMPTION = 0.1f; //equates to 1 circle
    private static Vector3 FINAL_POS = new Vector3(0.0f, 0.025f, 0.0f);

    private string CONTROL_NAME = "SHIP ALERT STATUS";
    private static string INFO_MESSAGE = "Determines ship status (normal, orange alert, red alert).";
    private List<string> CONTROL_DESCS = new List<string> { "LOWER", "ELEVATE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject selector_stick;
    public LightsManager lights_manager;
    private GameObject selector_indicator;

    private bool is_powered = false;
    private int curr_status = 0;
    private Coroutine status_shift_coroutine = null;
    private Coroutine power_loss_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public GameObject IK_target = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Grasp;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private void Start()
    {
        selector_indicator = selector_stick.transform.GetChild(0).GetChild(1).GetChild(0).gameObject;
        hud_info = new HUDInfo(CONTROL_NAME, true);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);
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

    public int getCurrColor()
    {
        return curr_status;
    }

    private void displayAdjustment()
    {
        //update rest of ship
        ReferenceAssistor.Instance.module_handlers[4].GetComponent<StatusIndicators>().displayShipStatus(COLOR_OPTIONS[curr_status]);

        //update indicator
        selector_indicator.GetComponent<UnityEngine.UI.RawImage>().color = COLOR_OPTIONS[curr_status];

        //change lights
        if (ReferenceAssistor.Instance.power_manager.getShipHasPower() == true)
        {
            if (curr_status == 2)
            {
                lights_manager.enableRedAlert();
            }
            else
            {
                lights_manager.disableRedAlert();
            }
        }

        //notify self destruct
        GetComponent<SelfDestruct>().onShipStatusChange();
    }

    IEnumerator statusShift()
    {
        float animation_time = MOVE_TIME;

        Vector3 starting_pos = selector_stick.transform.localPosition;
        Vector3 dest_pos = Vector3.Lerp(Vector3.zero, FINAL_POS, curr_status / 2.0f);

        //move slider
        while (animation_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            animation_time = Mathf.Max(0.0f, animation_time - dt);
            selector_stick.transform.localPosition = Vector3.Lerp(starting_pos, dest_pos, 1.0f - (animation_time / MOVE_TIME));

            yield return null;
        }

        if (curr_status > 0)
        {
            ReferenceAssistor.Instance.power_manager.controlPowerChange(3, this.GetType().Name, MAX_POWER_CONSUMPTION);
            hud_info.setPowerConsumption(MAX_POWER_CONSUMPTION);
        }
        else
        {
            ReferenceAssistor.Instance.power_manager.controlPowerChange(3, this.GetType().Name, 0.0f);
            hud_info.setPowerConsumption(0.0f);
        }

        displayAdjustment();

        BUTTONS[0].updateInteractable(curr_status > 0 && is_powered);
        BUTTONS[1].updateInteractable(curr_status < 2 && is_powered);
        BUTTONS[0].untoggle();
        BUTTONS[1].untoggle();

        status_shift_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        keys_down = inputs;
        if (status_shift_coroutine == null)
        {
            bool shifted = false;
            if (curr_status < 2)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], keys_down)) //shift up
                {
                    shifted = true;
                    BUTTONS[1].toggle();
                    BUTTONS[0].updateInteractable(false);
                    curr_status++;
                    transmitColorSelectionAdjustmentRPC(curr_status);
                }
            }
            if (shifted == false)
            {
                if (curr_status > 0)
                {
                    if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], keys_down)) //shift down
                    {
                        BUTTONS[0].toggle();
                        BUTTONS[1].updateInteractable(false);
                        curr_status--;
                        transmitColorSelectionAdjustmentRPC(curr_status);
                    }
                }
            }
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);

        Vector3 start_pos = selector_stick.transform.localPosition;
        float anim_time = power_off_time;
        curr_status = 0;
        displayAdjustment();
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            selector_stick.transform.localPosition = Vector3.Lerp(start_pos, Vector3.zero, 1.0f - (anim_time / power_off_time));
            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        selector_indicator.SetActive(true);
        BUTTONS[0].updateInteractable(curr_status > 0);
        BUTTONS[1].updateInteractable(curr_status < 2);
        BUTTONS[0].untoggle();
        BUTTONS[1].untoggle();
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        selector_indicator.SetActive(false);

        hud_info.setPowerConsumption(0.0f);

        //return to normal status
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitColorSelectionAdjustmentRPC(int cs)
    {
        curr_status = cs;
        if (status_shift_coroutine != null)
        {
            StopCoroutine(status_shift_coroutine);
        }
        status_shift_coroutine = StartCoroutine(statusShift());
    }
}