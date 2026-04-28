/*
    TorpedoTrigger.cs
    - Handles arming and firing of torpedoes
    - Moves base and lever accordingly
    Contributor(s): Jake Schott
    Last Updated: 3/26/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TorpedoTrigger : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float ARM_TIME = 1.0f;
    private static float COOLDOWN_TIME = 3.0f;
    private static float RED_BUTTON_PUSH_TIME = 0.5f;
    private static Vector3 TRIGGER_BASE_FINAL_POS = new Vector3(0.0f, -0.0148f, -0.0353f);

    private string CONTROL_NAME = "TORPEDO TRIGGER";
    private static string INFO_MESSAGE = "Used to fire torpedo in selected direction if torpedo is loaded in corresponding bay.";
    private List<string> CONTROL_DESCS = new List<string>{ "FIRE", "ARM" };
    private List<int> CONTROL_INDEXES = new List<int>(){ 6, 11 };
    private List<Button> BUTTONS = new List<Button>();

    public AudioSource torpedo_ready_sound;
    public List<AudioClip> torpedo_launch_sound_options = null;
    public GameObject torpedo_launch_sounds;
    public GameObject trigger_base;
    public GameObject trigger_green_light;
    public GameObject trigger_red_light;
    private TorpedoLauncher torpedo_launcher;
    private TorpedoLoader torpedo_loader;
    private TorpedoBaySelector torpedo_bay_selector;
    private TorpedoPowers torpedo_powers;

    private bool is_powered = false;
    private float trigger_percentage = 0.0f;
    private Vector3 trigger_base_initial_pos;
    private Coroutine trigger_arm_coroutine = null;
    private Coroutine after_torpedo_fire_coroutine = null;
    private Coroutine red_button_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public GameObject IK_target;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;

    private void Start()
    {
        torpedo_loader = ReferenceAssistor.Instance.module_handlers[2].GetComponent<TorpedoLoader>();
        torpedo_bay_selector = GetComponent<TorpedoBaySelector>();
        torpedo_powers = GetComponent<TorpedoPowers>();
        torpedo_launcher = GetComponent<TorpedoLauncher>();

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

        trigger_base_initial_pos = trigger_base.transform.localPosition;
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
    private void displayAdjustment()
    {
        float trigger_base_distance_percentage = Mathf.Min(1.0f, trigger_percentage / 0.8f);
        trigger_base.transform.localPosition = Vector3.Lerp(trigger_base_initial_pos, TRIGGER_BASE_FINAL_POS, trigger_base_distance_percentage);

        float trigger_lever_rotation = Mathf.Max(0.0f, (trigger_percentage - 0.5f) / 0.5f);
        trigger_base.transform.GetChild(0).localRotation = Quaternion.Euler(158.0f + (trigger_lever_rotation * -15f), 180.0f, 90.0f);

        //update lit indicators
        if (is_powered == true)
        {
            if (trigger_percentage >= 1.0f && torpedo_loader.getBayOccupant(torpedo_bay_selector.getDirectionIndex()) >= 0)
            {
                if (torpedo_ready_sound.isPlaying == false)
                {
                    torpedo_ready_sound.Play();
                }
                trigger_green_light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_green;
                trigger_red_light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_red;
            }
            else
            {
                trigger_green_light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_green;
                trigger_red_light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_red;
            }
        }
    }

    IEnumerator pushRedButton()
    {
        for (int i = 0; i <= 1; i++)
        {
            float half_time = RED_BUTTON_PUSH_TIME * 0.5f;
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

                trigger_base.transform.GetChild(0).GetChild(0).localPosition =
                    new Vector3(0, 0, Mathf.Lerp(0.0f, -0.004f, push_percentage));

                yield return null;
            }
        }

        red_button_coroutine = null;
    }

    IEnumerator afterTorpedoFire()
    {
        trigger_percentage = 1.0f;

        if (red_button_coroutine != null)
        {
            StopCoroutine(red_button_coroutine);
        }
        red_button_coroutine = StartCoroutine(pushRedButton());

        float cooldown_time = COOLDOWN_TIME;
        while (cooldown_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            cooldown_time = Mathf.Max(0.0f, cooldown_time - dt);

            float before_trigger_percentage = trigger_percentage;

            trigger_percentage = Mathf.Max(0.0f, ((trigger_percentage * COOLDOWN_TIME) - dt) / COOLDOWN_TIME);

            displayAdjustment();

            keys_down.Clear();
            yield return null;
        }

        BUTTONS[1].updateInteractable(is_powered);
        trigger_percentage = 0.0f;

        after_torpedo_fire_coroutine = null;
    }

    IEnumerator triggerArming()
    {
        while (keys_down.Count > 0 || trigger_percentage > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            float before_trigger_percentage = trigger_percentage;

            bool arming = PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], keys_down);
            if (arming == true && is_powered == true)
            {
                trigger_percentage = Mathf.Min(1.0f, ((trigger_percentage * ARM_TIME) + dt) / ARM_TIME);
            }
            else
            {
                trigger_percentage = Mathf.Max(0.0f, ((trigger_percentage * ARM_TIME) - dt) / ARM_TIME);
            }

            BUTTONS[0].updateInteractable(is_powered && trigger_percentage >= 1.0f && torpedo_loader.getBayOccupant(torpedo_bay_selector.getDirectionIndex()) >= 0);

            if (trigger_percentage != before_trigger_percentage)
            {
                transmitTriggerPercentageRPC(trigger_percentage);
            }

            keys_down.Clear();
            yield return null;
        }

        trigger_arm_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        keys_down = inputs;
        if (trigger_arm_coroutine == null && after_torpedo_fire_coroutine == null)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs))
            {
                trigger_arm_coroutine = StartCoroutine(triggerArming());
            }
        }
        else
        {
            if (torpedo_loader.getBayOccupant(torpedo_bay_selector.getDirectionIndex()) >= 0 && trigger_percentage >= 1.0f && after_torpedo_fire_coroutine == null)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
                {
                    BUTTONS[0].toggle(0.2f);
                    BUTTONS[1].updateInteractable(false);
                    transmitTorpedoFireRPC();
                }
            }
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(true);
        trigger_green_light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_green;
        trigger_red_light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_red;
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        trigger_green_light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_green;
        trigger_red_light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_red;
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTriggerPercentageRPC(float trig)
    {
        trigger_percentage = trig;
        displayAdjustment();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTorpedoFireRPC()
    {
        int current_bay = torpedo_bay_selector.getDirectionIndex();
        if (NetworkManager.Singleton.IsHost == true)
        {
            torpedo_launcher.fireTorpedo(current_bay, torpedo_loader.getBayOccupant(current_bay), torpedo_powers.getPowerLevel(current_bay));
        }

        //handle unload
        torpedo_loader.unloadTorpedo(current_bay);

        //play sound
        torpedo_launch_sounds.transform.GetChild(current_bay).GetComponent<AudioSource>().clip = torpedo_launch_sound_options[Random.Range(0, torpedo_launch_sound_options.Count)];
        torpedo_launch_sounds.transform.GetChild(current_bay).GetComponent<AudioSource>().Play();

        if (after_torpedo_fire_coroutine != null)
        {
            StopCoroutine(after_torpedo_fire_coroutine);
        }
        if (trigger_arm_coroutine != null)
        {
            StopCoroutine(trigger_arm_coroutine);
            trigger_arm_coroutine = null;
        }
        after_torpedo_fire_coroutine = StartCoroutine(afterTorpedoFire());
    }
}