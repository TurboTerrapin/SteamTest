/*
    SelfDestruct.cs
    - Used to handle code input and initation/abort
    Contributor(s): Jake Schott
    Last Updated: 10/23/2025
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SelfDestruct : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float DIGIT_PUSH_TIME = 0.2f;
    private static float SEQUENCE_PUSH_TIME = 0.5f;
    private static float SEQUENCE_COOLDOWN_TIME = 2.0f;

    private string[] CONTROL_NAMES = new string[] { "SELF-DESTRUCT CODE", "SELF-DESTRUCT SEQUENCE" };
    private static string INFO_MESSAGE = "Enables the self-destruct sequence which destroys the ship afer a 10-second countdown unless aborted.";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE", "INITATE", "ABORT" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5, 6, 12 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public GameObject destruct_display;
    public GameObject digit_buttons;
    public List<GameObject> sequence_buttons; //0 is initiate, 1 is abort

    private bool is_powered = false;
    private Vector3[] digit_initial_pos = new Vector3[8];
    private Vector3[] sequence_initial_pos = new Vector3[2];
    private Vector3 push_direction = new Vector3(-0.002f, -0.0053f, 0f);
    private int[] code = new int[] { 0, 0, 0, 0 };
    private Coroutine digit_adjustment_coroutine = null;
    private Coroutine sequence_change_coroutine = null;

    private List<string> ray_targets = new List<string> { "destruct_digit_a", "destruct_digit_b", "destruct_digit_c", "destruct_digit_d", "destruct_or_abort" };

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0]);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        hud_info.setButtons(BUTTON_LISTS[0]);
        hud_info.setInfo(INFO_MESSAGE);

        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[3], false, true));

        //set initial positions
        for (int i = 0; i < digit_buttons.transform.childCount; i++)
        {
            digit_initial_pos[i] = digit_buttons.transform.GetChild(i).localPosition;
        }
        for (int i = 0; i < sequence_buttons.Count; i++)
        {
            sequence_initial_pos[i] = sequence_buttons[i].transform.localPosition;
        }
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);

        if (index > 3)
        {
            index = 1;
        }
        else
        {
            index = 0;
        }
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index]);

        return hud_info;
    }

    IEnumerator sequenceAdjustment(int index)
    {
        //set buttons to initial positions
        for (int i = 0; i < sequence_buttons.Count; i++)
        {
            sequence_buttons[i].transform.localPosition = sequence_initial_pos[i];
        }

        Vector3 final_pos = sequence_initial_pos[index] + push_direction;

        for (int i = 0; i <= 1; i++)
        {
            float half_time = SEQUENCE_PUSH_TIME * 0.5f;
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

                sequence_buttons[index].transform.localPosition =
                    new Vector3(Mathf.Lerp(sequence_initial_pos[index].x, final_pos.x, push_percentage),
                                Mathf.Lerp(sequence_initial_pos[index].y, final_pos.y, push_percentage),
                                Mathf.Lerp(sequence_initial_pos[index].z, final_pos.z, push_percentage));

                yield return null;
            }
        }

        yield return new WaitForSeconds(SEQUENCE_COOLDOWN_TIME);

        BUTTON_LISTS[1][0].updateInteractable(is_powered);
        BUTTON_LISTS[1][1].updateInteractable(is_powered);

        sequence_change_coroutine = null;
    }

    IEnumerator digitAdjustment(int index)
    {
        //set buttons to initial positions
        for (int i = 0; i < digit_buttons.transform.childCount; i++)
        {
            digit_buttons.transform.GetChild(i).localPosition = digit_initial_pos[i];
        }

        Vector3 final_pos = digit_initial_pos[index] + push_direction;

        for (int i = 0; i <= 1; i++)
        {
            float half_time = DIGIT_PUSH_TIME * 0.5f;
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

                digit_buttons.transform.GetChild(index).localPosition =
                    new Vector3(Mathf.Lerp(digit_initial_pos[index].x, final_pos.x, push_percentage),
                                Mathf.Lerp(digit_initial_pos[index].y, final_pos.y, push_percentage),
                                Mathf.Lerp(digit_initial_pos[index].z, final_pos.z, push_percentage));

                yield return null;
            }

            if (i == 0)
            {
                displayCodeAdjustment();
            }
        }

        BUTTON_LISTS[0][0].updateInteractable(is_powered);
        BUTTON_LISTS[0][1].updateInteractable(is_powered);

        digit_adjustment_coroutine = null;
    }

    private void displayCodeAdjustment()
    {
        for (int i = 0; i <= 3; i++)
        {
            destruct_display.transform.GetChild(i).GetComponent<TMP_Text>().SetText(code[i].ToString());
        }
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int digit = ray_targets.IndexOf(current_target.name);

        if (digit > 3)
        {
            if (sequence_change_coroutine == null && digit_adjustment_coroutine == null)
            {
                for (int i = 2; i <= 3; i++)
                {
                    if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                    {
                        BUTTON_LISTS[1][i - 2].toggle(0.1f);
                        BUTTON_LISTS[1][0].updateInteractable(false);
                        BUTTON_LISTS[1][1].updateInteractable(false);
                        transmitDestructSequenceChangeRPC(i - 2);
                        return;
                    }
                }
            }
            return;
        }

        if (digit_adjustment_coroutine == null && sequence_change_coroutine == null)
        {
            for (int i = 0; i <= 1; i++)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    int button_index = digit;
                    BUTTON_LISTS[0][i].toggle(0.1f);
                    if (i == 0)
                    {
                        button_index += 4;
                        code[digit]--;
                        if (code[digit] < 0)
                        {
                            code[digit] = 9;
                        }
                        BUTTON_LISTS[0][1].updateInteractable(false);
                    }
                    else
                    {
                        code[digit]++;
                        if (code[digit] > 9)
                        {
                            code[digit] = 0;
                        }
                        BUTTON_LISTS[0][0].updateInteractable(false);
                    }
                    transmitDigitChangeRPC(button_index, code[0], code[1], code[2], code[3]);
                    return;
                }
            }
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        destruct_display.SetActive(true);
        BUTTON_LISTS[0][0].updateInteractable(true);
        BUTTON_LISTS[0][1].updateInteractable(true);
        BUTTON_LISTS[1][0].updateInteractable(true);
        BUTTON_LISTS[1][1].updateInteractable(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        destruct_display.SetActive(false);
        BUTTON_LISTS[0][0].updateInteractable(false);
        BUTTON_LISTS[0][1].updateInteractable(false);
        BUTTON_LISTS[1][0].updateInteractable(false);
        BUTTON_LISTS[1][1].updateInteractable(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDestructSequenceChangeRPC(int button_index)
    {
        if (sequence_change_coroutine != null)
        {
            StopCoroutine(sequence_change_coroutine);
        }
        sequence_change_coroutine = StartCoroutine(sequenceAdjustment(button_index));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDigitChangeRPC(int button_index, int a, int b, int c, int d)
    {
        code[0] = a;
        code[1] = b;
        code[2] = c;
        code[3] = d;
        if (digit_adjustment_coroutine != null)
        {
            StopCoroutine(digit_adjustment_coroutine);
        }
        digit_adjustment_coroutine = StartCoroutine(digitAdjustment(button_index));
    }
}