/*
    PowerRegulationModuleA.cs
    - Handles the knob-turning mini-game in the engineer position
    Contributor(s): Jake Schott
    Last Updated: 5/9/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerRegulationModuleA : NetworkBehaviour, IControllable, IPowerRegulable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float STATE_CHANGE_TIME = 0.5f;
    private static float ROTATE_SPEED = 75.0f;
    private static float[] ARC_STAGE_SIZES = new float[3] { 0.4f, 0.25f, 0.15f };

    private string[] CONTROL_NAMES = new string[3] { "PRIMARY SENSOR ANGLE", "SECONDARY SENSOR ANGLE", "TERTIARY SENSOR ANGLE" };
    private static string INFO_MESSAGE = "Align the corresponding colors to their arcs to complete the module.";
    private List<string> CONTROL_DESCS = new List<string> { "ROTATE LEFT", "ROTATE RIGHT" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3]{ new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject prsa_display;
    public List<GameObject> prsa_color_identifiers = null;
    public List<GameObject> prsa_knobs = null;

    private PowerRegulator power_regulator;
    private List<GameObject> color_dots = new List<GameObject>();
    private List<GameObject> color_arcs = new List<GameObject>();

    private bool currently_active = false;
    private int stage = 0;
    private float[] dot_rotations = new float[3] { 0.0f, 0.0f, 0.0f };
    private float[] arc_rotations = new float[3] { 0.0f, 0.0f, 0.0f };
    private float[] arc_sizes = new float[3] { 0.25f, 0.25f, 0.25f };
    private Coroutine state_change_coroutine = null;

    private List<string> ray_targets = new List<string> { "prsa_knob_green", "prsa_knob_purple", "prsa_knob_orange" };

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public List<GameObject> hand_placements = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private void Start()
    {
        power_regulator = GameObject.Find("PowerHandler").GetComponent<PowerRegulator>();
        for (int i = 0; i < 3; i++)
        {
            color_dots.Add(prsa_display.transform.GetChild(i).GetChild(1).gameObject);
            color_arcs.Add(prsa_display.transform.GetChild(i).GetChild(0).gameObject);
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        }

        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        hud_info.setButtons(BUTTON_LISTS[0]);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 7);

        return hud_info;
    }

    public Transform getIKTarget(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        int offset = index;

        float shortestDistance;
        int shortestIndex = offset * 4;
        shortestDistance = Vector3.Distance(hand_placements[offset].transform.position, IK_targets[shortestIndex].transform.position);

        int topSearchBound = shortestIndex + 3;

        for (int i = offset * 4; i <= topSearchBound; i++)
        {
            float distance = Vector3.Distance(hand_placements[offset].transform.position, IK_targets[i].transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                shortestIndex = i;
            }
        }
        
        return IK_targets[shortestIndex].transform;
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

    private void changeRingState(GameObject to_change, bool solid)
    {
        Color ring_color = to_change.GetComponent<UnityEngine.UI.Image>().color;
        float a = 0.2f;
        if (solid == true)
        {
            a = 1.0f;
        }

        to_change.GetComponent<UnityEngine.UI.Image>().color = new Color(ring_color.r, ring_color.g, ring_color.b, a);
    }

    private bool checkDotWithinArc(int dot)
    {
        float lower_bound = arc_rotations[dot];
        float upper_bound = arc_rotations[dot] + (arc_sizes[dot] * 360.0f);
        if (upper_bound > 360.0f && dot_rotations[dot] < lower_bound)
        {
            return ((dot_rotations[dot] + 360.0f) < upper_bound);
        }
        return ((dot_rotations[dot] > lower_bound) && (dot_rotations[dot] < upper_bound));
    }

    private void displayAdjustment(int to_display)
    {
        //adjust dot and arc
        color_dots[to_display].transform.localRotation = Quaternion.Euler(0.0f, 0.0f, dot_rotations[to_display]);
        color_arcs[to_display].transform.localRotation = Quaternion.Euler(0.0f, 0.0f, arc_rotations[to_display]);
        color_arcs[to_display].GetComponent<UnityEngine.UI.Image>().fillAmount = arc_sizes[to_display];

        //if within range, solidify arc
        bool dot_is_within_arc = checkDotWithinArc(to_display);
        changeRingState(color_arcs[to_display], dot_is_within_arc);

        //rotate knob
        prsa_knobs[to_display].transform.localRotation = Quaternion.Euler(-55.0f, -45.0f, 90.0f + dot_rotations[to_display]);
    }

    //sets the state 
    IEnumerator stateChangeHelper(bool to_change_to)
    {
        float anim_time = STATE_CHANGE_TIME;
        float[] starting_rotations = new float[3] { dot_rotations[0], dot_rotations[1], dot_rotations[2] };
        float[] destination_rotations = new float[3] { 0.0f, 0.0f, 0.0f };

        for (int i = 0; i < 3; i++)
        {
            if (dot_rotations[i] > 180.0f)
            {
                destination_rotations[i] = 360.0f;
            }
        }

        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            for (int i = 0; i < 3; i++)
            {
                //adjust slider
                float turn_percentage = Mathf.Lerp(destination_rotations[i], starting_rotations[i], (anim_time / STATE_CHANGE_TIME));
                dot_rotations[i] = turn_percentage;
                prsa_knobs[i].transform.localRotation = Quaternion.Euler(-55.0f, -45.0f, 90.0f + dot_rotations[i]);
            }

            yield return null;
        }

        prsa_display.SetActive(to_change_to);

        for (int i = 0; i < 3; i++)
        {
            dot_rotations[i] = 0.0f;
            displayAdjustment(i);
        }
        currently_active = to_change_to;

        state_change_coroutine = null;
    }

    private void handleNewStage()
    {
        float[] new_arc_rotations = new float[3] { 0.0f, 0.0f, 0.0f };
        for (int i = 0; i < 3; i++)
        {
            new_arc_rotations[i] = Random.Range(0.0f, 359.9f);
        }
        stageChangeRPC(stage + 1, new_arc_rotations[0], new_arc_rotations[1], new_arc_rotations[2]);
    }

    private void resetStateChangeCoroutine()
    {
        if (state_change_coroutine != null)
        {
            StopCoroutine(state_change_coroutine);
            state_change_coroutine = null;
        }
    }

    public void resetToDefault()
    {
        if (currently_active == false)
        {
            return;
        }
        currently_active = false;
        prsa_display.SetActive(false);
        for (int i = 0; i < 3; i++)
        {
            prsa_color_identifiers[i].SetActive(false);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
        }
        resetStateChangeCoroutine();
        state_change_coroutine = StartCoroutine(stateChangeHelper(false));
    }

    public void unlockControl()
    {
        if (currently_active == true)
        {
            return;
        }
        currently_active = true;
        stage = -1;

        if (NetworkManager.Singleton.IsHost == true)
        {
            handleNewStage();
        }
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (currently_active == false)
        {
            return;
        }

        int target_index = ray_targets.IndexOf(current_target.name);

        int knob_direction = 0;
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to increment
        {
            knob_direction += 1;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //Q to decrement
        {
            knob_direction -= 1;
        }
        if (knob_direction != 0)
        {
            float rot = dot_rotations[target_index];
            if (knob_direction > 0)
            {
                rot += dt * ROTATE_SPEED;
            }
            else
            {
                rot -= dt * ROTATE_SPEED;
            }
            if (rot >= 360.0f)
            {
                rot -= 360.0f;
            }
            else if (rot < 0.0f)
            {
                rot += 360.0f;
            }
            rotationChangeRPC(target_index, rot);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void stageChangeRPC(int new_stage, float nar_a, float nar_b, float nar_c)
    {
        stage = new_stage;
        arc_rotations[0] = nar_a;
        arc_rotations[1] = nar_b;
        arc_rotations[2] = nar_c;
        prsa_display.SetActive(true);
        for (int i = 0; i < 3; i++)
        {
            arc_sizes[i] = ARC_STAGE_SIZES[new_stage];
            displayAdjustment(i);
            prsa_color_identifiers[i].SetActive(true);
            BUTTON_LISTS[i][0].updateInteractable(true);
            BUTTON_LISTS[i][1].updateInteractable(true);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void rotationChangeRPC(int dot, float new_rotation)
    {
        dot_rotations[dot] = new_rotation;
        displayAdjustment(dot);

        if (checkDotWithinArc(dot) == true)
        {
            power_regulator.playCorrectSound();
        }

        if (NetworkManager.Singleton.IsHost == true)
        {
            bool stage_completed = true;
            for (int i = 0; i < 3; i++)
            {
                if (checkDotWithinArc(i) == false)
                {
                    stage_completed = false;
                    break;
                }
            }

            if (stage_completed == true)
            {
                if (stage == 2)
                {
                    transmitModuleCompletionRPC();
                }
                else
                {
                    handleNewStage();
                }
            }
        }
    }

    //called by host when mini-game completed
    [Rpc(SendTo.Everyone)]
    private void transmitModuleCompletionRPC()
    {
        power_regulator.moduleCompleted(this.GetType().Name);
    }
}
