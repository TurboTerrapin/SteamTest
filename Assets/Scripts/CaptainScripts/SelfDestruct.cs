/*
    SelfDestruct.cs
    - Used to handle code input and initation/abort
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SelfDestruct : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float DIGIT_CHANGE_TIME = 0.2f;
    private static float CORRECT_CODE_FLASH_TIME = 0.15f;
    private static float SEQUENCE_TURN_TIME = 0.5f;
    private static int DESTRUCT_COUNTDOWN_TIME = 10;

    private string[] CONTROL_NAMES = new string[] { "SELF-DESTRUCT CODE", "SELF-DESTRUCT SEQUENCE" };
    private static string INFO_MESSAGE = "Enables the self-destruct sequence which destroys the ship afer a 10-second countdown unless aborted.";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE", "INITIATE", "ABORT" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5, 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public GameObject destruct_display;
    public GameObject digit_switches;
    public GameObject self_destruct_dial;

    private bool is_powered = false;
    private int[] input_code = new int[] { 0, 0, 0, 0 };
    private int[] correct_code = new int[] { 1, 9, 8, 4 };
    private bool code_is_currently_correct = false;
    private Coroutine digit_adjustment_coroutine = null;
    private Coroutine correct_code_flasher_coroutine = null;
    private Coroutine sequence_change_coroutine = null;
    private Coroutine destruct_coroutine = null;

    private List<string> ray_targets = new List<string> { "destruct_digit_a", "destruct_digit_b", "destruct_digit_c", "destruct_digit_d", "destruct_or_abort" };

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Grasp;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    public float lerp_speed = 5f;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0]);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        hud_info.setButtons(BUTTON_LISTS[0]);
        hud_info.setInfo(INFO_MESSAGE);

        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, true));
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);

        if (index > 3)
        {
            index = 1;
            hud_info.setButtons(BUTTON_LISTS[index], 6);
        }
        else
        {
            index = 0;
            hud_info.setButtons(BUTTON_LISTS[index], 7);
        }
        hud_info.setTitle(CONTROL_NAMES[index]);

        return hud_info;
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
    private void showDestructCountdown(bool to_show)
    {
        destruct_display.transform.GetChild(0).gameObject.SetActive(!to_show);
        destruct_display.transform.GetChild(2).gameObject.SetActive(to_show);
        destruct_display.transform.GetChild(3).gameObject.SetActive(to_show);
    }

    IEnumerator selfDestructCountdown()
    {
        infoColorAdjustment(new Color(1.0f, 0.0f, 0.0f));
        destructSymbolColorAdjustment(new Color(1.0f, 0.0f, 0.0f));
        showDestructCountdown(true);

        float elapsed_time = 0.0f;
        float anim_time = DESTRUCT_COUNTDOWN_TIME;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);
            elapsed_time += dt;

            float a = Mathf.PingPong(elapsed_time, 0.2f);

            if (a > 0.1f)
            {
                a = 1.0f;
            }
            else
            {
                a = 0.2f;
            }

            Color c = new Color(1.0f, 0.0f, 0.0f, a);
            infoColorAdjustment(c);
            destructSymbolColorAdjustment(c);

            destruct_display.transform.GetChild(2).GetComponent<UnityEngine.UI.Image>().fillAmount = 1.0f - (anim_time / DESTRUCT_COUNTDOWN_TIME);
            destruct_display.transform.GetChild(3).GetComponent<TMP_Text>().SetText(Mathf.CeilToInt(anim_time).ToString());

            yield return null;
        }

        //one-second courtesy
        yield return new WaitForSeconds(1.0f);

        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().endScenario(ScenarioManager.EndCondition.SelfDestructed);
        }
    }

    IEnumerator sequenceAdjustment(bool destructing)
    {
        BUTTON_LISTS[0][0].updateInteractable(false);
        BUTTON_LISTS[0][1].updateInteractable(false);

        float anim_time = SEQUENCE_TURN_TIME;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            float switch_percentage = anim_time / SEQUENCE_TURN_TIME;
            if (destructing == false)
            {
                switch_percentage = 1.0f - switch_percentage;
            }

            self_destruct_dial.transform.localRotation =
                Quaternion.Euler(self_destruct_dial.transform.localEulerAngles.x,
                                 self_destruct_dial.transform.localEulerAngles.y,
                                 Mathf.Lerp(0.0f, 90.0f, switch_percentage));

            yield return null;
        }

        if (destruct_coroutine != null)
        {
            StopCoroutine(destruct_coroutine);
            destruct_coroutine = null;
        }
        if (destructing == true)
        {
            BUTTON_LISTS[1][0].updateDesc(CONTROL_DESCS[3]);
            if (correct_code_flasher_coroutine != null)
            {
                StopCoroutine(correct_code_flasher_coroutine);
                correct_code_flasher_coroutine = null;
            }

            destruct_coroutine = StartCoroutine(selfDestructCountdown());
        }
        else
        {
            BUTTON_LISTS[1][0].updateDesc(CONTROL_DESCS[2]);
            showDestructCountdown(false);
            infoColorAdjustment(new Color(0.0f, 0.84f, 1.0f));
            destructSymbolColorAdjustment(new Color(0.0f, 0.84f, 1.0f, 0.2f));
        }

        BUTTON_LISTS[0][0].updateInteractable(is_powered && destructing == false);
        BUTTON_LISTS[0][1].updateInteractable(is_powered && destructing == false);

        sequence_change_coroutine = null;
        displayCodeAdjustment();
    }

    IEnumerator digitAdjustment(int index, bool increase)
    {
        for (int i = 0; i < 2; i++)
        {
            BUTTON_LISTS[0][i].updateInteractable(false);
        }

        float initial_rotation = -67.0f;
        float destination_rotation = -44.0f;

        if (increase == true) //up
        {
            destination_rotation = -90.0f;
        }

        float anim_time = DIGIT_CHANGE_TIME;
        for (int i = 0; i < 2; i++)
        {
            float half_time = DIGIT_CHANGE_TIME * 0.5f;
            float curr_time = half_time;

            while (curr_time > 0.0f)
            {
                curr_time = Mathf.Max(0.0f, curr_time - Time.deltaTime);

                float switch_percentage = 1.0f - (curr_time / half_time);
                if (i == 1)
                {
                    switch_percentage = (curr_time / half_time);
                }

                digit_switches.transform.GetChild(index).localRotation = Quaternion.Euler(Mathf.Lerp(initial_rotation, destination_rotation, switch_percentage), 90.0f, 0.0f);

                yield return null;
            }

            if (i == 0)
            {
                displayCodeAdjustment();
            }
        }

        BUTTON_LISTS[0][0].updateInteractable(is_powered && sequence_change_coroutine == null && destruct_coroutine == null);
        BUTTON_LISTS[0][1].updateInteractable(is_powered && sequence_change_coroutine == null && destruct_coroutine == null);

        digit_adjustment_coroutine = null;
    }

    private void infoColorAdjustment(Color c)
    {
        for (int i = 0; i < 4; i++)
        {
            destruct_display.transform.GetChild(0).GetChild(i).GetComponent<TMP_Text>().color = c;
        }
        for (int i = 0; i < 2; i++)
        {
            destruct_display.transform.GetChild(i + 4).GetComponent<UnityEngine.UI.RawImage>().color = c;
        }
    }
    
    private void destructSymbolColorAdjustment(Color c)
    {
        destruct_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = c;
        for (int i = 0; i < 2; i++)
        {
            destruct_display.transform.GetChild(1).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = c;
        }
    }

    IEnumerator correctCodeFlasher()
    {
        float elapsed_time = 0.0f;
        while (true)
        {
            elapsed_time += Time.deltaTime;

            float a = Mathf.PingPong(elapsed_time, CORRECT_CODE_FLASH_TIME);

            if (a > CORRECT_CODE_FLASH_TIME / 2.0f)
            {
                a = 1.0f;
            }
            else
            {
                a = 0.2f;
            }

            Color c = new Color(0.0f, 0.84f, 1.0f, a);
            infoColorAdjustment(c);
            destructSymbolColorAdjustment(c);

            yield return null;
        }
    }

    public void setNewCode(string new_code)
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            transmitNewCodeRPC(new_code);
        }
    }

    public void onShipStatusChange()
    {
        //update interactability
        displayCodeAdjustment();

        //if currently destructing and no longer red alert, end destruct countdown
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (destruct_coroutine != null && GetComponent<ShipStatus>().getCurrColor() < 2)
            {
                transmitDestructSequenceChangeRPC(false);
            }
        }
    }

    private void checkCodeCorrectness()
    {
        //check for correct code
        code_is_currently_correct = (DataConverter.arrayToString(input_code).CompareTo(DataConverter.arrayToString(correct_code)) == 0);

        //flash if correct code
        if (destruct_coroutine == null)
        {
            if (code_is_currently_correct == true && correct_code_flasher_coroutine == null)
            {
                correct_code_flasher_coroutine = StartCoroutine(correctCodeFlasher());
            }
            else if (code_is_currently_correct == false && correct_code_flasher_coroutine != null)
            {
                StopCoroutine(correct_code_flasher_coroutine);
                correct_code_flasher_coroutine = null;
                infoColorAdjustment(new Color(0.0f, 0.84f, 1.0f));
                destructSymbolColorAdjustment(new Color(0.0f, 0.84f, 1.0f, 0.2f));
            }
        }
    }

    private void displayCodeAdjustment()
    {
        //update digits
        for (int i = 0; i < 4; i++)
        {
            destruct_display.transform.GetChild(0).GetChild(i).GetComponent<TMP_Text>().SetText(input_code[i].ToString());
        }

        checkCodeCorrectness();
        bool can_do_sequence_adjustment = (is_powered && sequence_change_coroutine == null);
        can_do_sequence_adjustment = ((code_is_currently_correct && GetComponent<ShipStatus>().getCurrColor() == 2) || destruct_coroutine != null);
        BUTTON_LISTS[1][0].updateInteractable(can_do_sequence_adjustment);
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int index = ray_targets.IndexOf(current_target.name);

        //self-destruct initiate/abort
        if (index > 3) 
        {
            if (digit_adjustment_coroutine == null && sequence_change_coroutine == null)
            {
                if (destruct_coroutine != null || (code_is_currently_correct == true && GetComponent<ShipStatus>().getCurrColor() == 2))
                {
                    if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[2], inputs))
                    {
                        BUTTON_LISTS[1][0].toggle(0.2f);
                        BUTTON_LISTS[1][0].updateInteractable(false);
                        transmitDestructSequenceChangeRPC(destruct_coroutine == null);
                        return;
                    }
                }
            }
            return;
        }

        //digit adjustment
        if (digit_adjustment_coroutine == null && sequence_change_coroutine == null && destruct_coroutine == null) 
        {
            for (int i = 0; i < 2; i++)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    BUTTON_LISTS[0][i].toggle(0.1f);
                    bool increase = true;
                    if (i == 0)
                    {
                        increase = false;
                        input_code[index]--;
                        if (input_code[index] < 0)
                        {
                            input_code[index] = 9;
                        }
                    }
                    else
                    {
                        input_code[index]++;
                        if (input_code[index] > 9)
                        {
                            input_code[index] = 0;
                        }
                    }
                    transmitDigitChangeRPC(index, increase, DataConverter.arrayToString(input_code));
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
        displayCodeAdjustment();
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        destruct_display.SetActive(false);
        BUTTON_LISTS[0][0].updateInteractable(false);
        BUTTON_LISTS[0][1].updateInteractable(false);
        BUTTON_LISTS[1][0].updateInteractable(false);

        if (destruct_coroutine != null)
        {
            StopCoroutine(destruct_coroutine);
            destruct_coroutine = null;
        }

        if (sequence_change_coroutine != null)
        {
            StopCoroutine(sequence_change_coroutine);
            sequence_change_coroutine = null;
        }

        if (correct_code_flasher_coroutine != null)
        {
            StopCoroutine(correct_code_flasher_coroutine);
            correct_code_flasher_coroutine = null;
        }

        showDestructCountdown(false);
        infoColorAdjustment(new Color(0.0f, 0.84f, 1.0f));
        destructSymbolColorAdjustment(new Color(0.0f, 0.84f, 1.0f, 0.2f));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDestructSequenceChangeRPC(bool destructing)
    {
        if (sequence_change_coroutine != null)
        {
            StopCoroutine(sequence_change_coroutine);
        }
        sequence_change_coroutine = StartCoroutine(sequenceAdjustment(destructing));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDigitChangeRPC(int button_index, bool increase, string code)
    {
        input_code = DataConverter.stringToArray(code);
        if (digit_adjustment_coroutine != null)
        {
            StopCoroutine(digit_adjustment_coroutine);
        }
        digit_adjustment_coroutine = StartCoroutine(digitAdjustment(button_index, increase));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitNewCodeRPC(string new_code)
    {
        int[] new_code_as_array = DataConverter.stringToArray(new_code);
        correct_code = DataConverter.stringToArray(new_code);

        if (destruct_coroutine == null && sequence_change_coroutine == null)
        {
            displayCodeAdjustment();
        }
    }
}