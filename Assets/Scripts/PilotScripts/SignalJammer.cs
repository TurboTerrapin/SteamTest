/*
    SignalJammer.cs
    - Meant to temporarily jam signals
    - Does nothing
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SignalJammer : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float JAM_TIME = 10.0f; //seconds
    private static float RESET_TIME = 15.0f; //seconds
    private static float BUTTON_PUSH_TIME = 1.0f; //seconds
    private static Vector3 BUTTON_FINAL_POS = new Vector3(-2.4599f, -0.6773f, 2.1674f);
    private static float BAR_ANIMATION_TIME = 0.2f; //bars change every 0.2 seconds
    private static Color BLUE = new Color(0.0f, 0.84f, 1.0f, 1.0f);
    private static Color RED = new Color(1.0f, 0.0f, 0.0f, 1.0f);
    private static float MAX_POWER_CONSUMPTION = 0.2f; //equates to 2 circles

    private string CONTROL_NAME = "SIGNAL JAMMER";
    private static string INFO_MESSAGE = "Disrupts the ability of others to transmit signals and utilize location-tracking technology.";
    private List<string> CONTROL_DESCS = new List<string>() { "ACTIVATE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject signal_jam_button;
    public GameObject signal_jam_display;
    public GameObject signal_indicators;

    [Header("IK Targetable Details")]
    public GameObject IK_target;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Grasp;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;

    private bool is_powered = false;
    private float jam_time = 0.0f;
    private Coroutine signal_jam_coroutine = null;
    private Coroutine bars_animation_coroutine = null;
    private Vector3 button_initial_pos;

    private static HUDInfo hud_info = null;

    private void Start()
    {
        button_initial_pos = signal_jam_button.transform.localPosition;

        hud_info = new HUDInfo(CONTROL_NAME, true);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
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

    //changes the colors of the screen's border and lines
    private void colorChange(Color to_change_to)
    {
        GameObject border = signal_jam_display.transform.GetChild(0).gameObject;
        GameObject lines = signal_jam_display.transform.GetChild(1).gameObject;
        border.GetComponent<UnityEngine.UI.RawImage>().color = to_change_to;
        for (int i = 0; i < lines.transform.childCount; i++)
        {
            GameObject line = lines.transform.GetChild(i).gameObject;
            line.GetComponent<UnityEngine.UI.RawImage>().color = to_change_to;
            line.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = to_change_to;
            line.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = to_change_to;
        }
    }

    //sizes the bar at the index to the to_size_to input
    private void resizeBar(int index, float to_size_to)
    {
        GameObject lines = signal_jam_display.transform.GetChild(1).gameObject;
        lines.transform.GetChild(index).GetComponent<RectTransform>().sizeDelta = new Vector2(0.003f + to_size_to * 2, 0.005f);
        lines.transform.GetChild(index).GetChild(0).localPosition = new Vector3(0.0015f + to_size_to, 0.0f, 0.0f);
        lines.transform.GetChild(index).GetChild(1).localPosition = new Vector3(-0.0015f - to_size_to, 0.0f, 0.0f);
    }

    IEnumerator barAnimation()
    {
        GameObject lines = signal_jam_display.transform.GetChild(1).gameObject;
        while (true)
        {
            float anim_time = BAR_ANIMATION_TIME;
            float[] starting_sizes = new float[lines.transform.childCount];
            float[] sizes = new float[lines.transform.childCount];
            for (int i = 0; i < sizes.Length; i++)
            {
                starting_sizes[i] = lines.transform.GetChild(i).GetChild(0).localPosition.x - 0.0015f;
                sizes[i] = Random.Range(0.0f, 1.0f) * 0.005f;
            }
            while (anim_time > 0.0f)
            {
                float dt = Time.deltaTime;
                anim_time = Mathf.Max(0.0f, anim_time - dt);
                for (int i = 0; i < sizes.Length; i++)
                {
                    float to_size_to = Mathf.Lerp(starting_sizes[i], sizes[i], 1.0f - (anim_time / BAR_ANIMATION_TIME));
                    resizeBar(i, to_size_to);
                }
                yield return null;
            }
        }
    }

    IEnumerator resetBars()
    {
        GameObject lines = signal_jam_display.transform.GetChild(1).gameObject;
        float anim_time = BAR_ANIMATION_TIME;
        float[] starting_sizes = new float[lines.transform.childCount];
        float[] sizes = new float[lines.transform.childCount];
        for (int i = 0; i < sizes.Length; i++)
        {
            starting_sizes[i] = lines.transform.GetChild(i).GetChild(0).localPosition.x - 0.0015f;
            sizes[i] = 0.0f;
        }
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);
            for (int i = 0; i < sizes.Length; i++)
            {
                float to_size_to = Mathf.Lerp(starting_sizes[i], sizes[i], 1.0f - (anim_time / BAR_ANIMATION_TIME));
                resizeBar(i, to_size_to);
            }
            yield return null;
        }
    }

    IEnumerator signalJam()
    {
        //button push
        for (int i = 0; i <= 1; i++)
        {
            float half_time = BUTTON_PUSH_TIME * 0.5f;
            float push_time = half_time;

            while (push_time > 0.0f)
            {
                float dt = Time.deltaTime;
                push_time = Mathf.Max(0.0f, push_time - dt);

                float push_percentage = 1.0f - (push_time / half_time);
                if (i == 1)
                {
                    push_percentage = (push_time / half_time);
                }

                signal_jam_button.transform.localPosition = Vector3.Lerp(button_initial_pos, BUTTON_FINAL_POS, push_percentage);

                yield return null;
            }
        }
        BUTTONS[0].untoggle();

        //start signal jam
        if (bars_animation_coroutine != null)
        {
            StopCoroutine(bars_animation_coroutine);
        }
        bars_animation_coroutine = StartCoroutine(barAnimation());

        colorChange(RED);
        if (is_powered == true)
        {
            for (int i = 0; i < signal_indicators.transform.childCount; i++)
            {
                signal_indicators.transform.GetChild(i).GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_red;
            }
        }

        ReferenceAssistor.Instance.power_manager.controlPowerChange(0, this.GetType().Name, MAX_POWER_CONSUMPTION);
        hud_info.setPowerConsumption(MAX_POWER_CONSUMPTION);

        jam_time = JAM_TIME;
        while (jam_time > 0.0f)
        {
            float dt = Time.deltaTime;
            jam_time = Mathf.Max(0.0f, jam_time - dt);

            if (is_powered == true)
            {
                for (int i = 0; i < signal_indicators.transform.childCount; i++)
                {
                    if ((jam_time / JAM_TIME) <= (i * 1.0f / signal_indicators.transform.childCount))
                    {
                        signal_indicators.transform.GetChild(i).GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_neon;
                    }
                    else
                    {
                        signal_indicators.transform.GetChild(i).GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_red;
                    }
                }
            }

            yield return null;
        }

        StopCoroutine(bars_animation_coroutine);
        bars_animation_coroutine = StartCoroutine(resetBars());
        colorChange(BLUE);

        ReferenceAssistor.Instance.power_manager.controlPowerChange(0, this.GetType().Name, 0.0f);
        hud_info.setPowerConsumption(0.0f);

        float reset_time = RESET_TIME;
        while (reset_time > 0.0f)
        {
            float dt = Time.deltaTime;
            reset_time = Mathf.Max(0.0f, reset_time - dt);

            if (is_powered == true)
            {
                for (int i = 0; i < signal_indicators.transform.childCount; i++)
                {
                    if ((reset_time / RESET_TIME) <= (i * 1.0f / signal_indicators.transform.childCount))
                    {
                        signal_indicators.transform.GetChild(signal_indicators.transform.childCount - 1 - i).GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_neon;
                    }
                    else
                    {
                        signal_indicators.transform.GetChild(signal_indicators.transform.childCount - 1 - i).GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_neon;
                    }
                }
            }

            yield return null;
        }

        BUTTONS[0].updateInteractable(is_powered);

        bars_animation_coroutine = null;
        signal_jam_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (signal_jam_coroutine == null && is_powered == true)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTONS[0].toggle();
                transmitSignalJamRPC();
            }
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        signal_jam_display.SetActive(true);
        if (signal_jam_coroutine == null)
        {
            BUTTONS[0].updateInteractable(true);
            for (int i = 0; i < signal_indicators.transform.childCount; i++)
            {
                signal_indicators.transform.GetChild(i).GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_neon;
            }
        }
        else
        {
            if (jam_time > 0.0f)
            {
                ReferenceAssistor.Instance.power_manager.controlPowerChange(0, this.GetType().Name, MAX_POWER_CONSUMPTION);
                hud_info.setPowerConsumption(MAX_POWER_CONSUMPTION);
            }
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        signal_jam_display.SetActive(false);
        BUTTONS[0].updateInteractable(false);
        for (int i = 0; i < signal_indicators.transform.childCount; i++)
        {
            signal_indicators.transform.GetChild(i).GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_neon;
        }
        hud_info.setPowerConsumption(0.0f);
    }


    [Rpc(SendTo.Everyone)]
    private void transmitSignalJamRPC()
    {
        if (signal_jam_coroutine != null)
        {
            StopCoroutine(signal_jam_coroutine);
        }
        signal_jam_coroutine = StartCoroutine(signalJam());
    }
}