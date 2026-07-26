/*
    EncryptionKeys.cs
    - Moves encryption key levers
    - Adjusts encryption key screens
    Contributor(s): Jake Schott
    Last Updated: 7/23/2026
*/

using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class EncryptionKeys : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 0.15f;
    private static Vector3 FINAL_LEVER_DIRECTION = new Vector3(0.074f, 0.027f, 0.0f); //key 99

    private string[] CONTROL_NAMES = new string[] { "ENCRYPTION KEY BLUE", "ENCRYPTION KEY PURPLE", "ENCRYPTION KEY ORANGE", "ENCRYPTION KEY GREEN" };
    private static string INFO_MESSAGE = "Handles adjustment of two-digit encryption keys used for computer procedures.";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[4] { new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject encryption_key_levers;
    public GameObject encryption_key_glasses;

    private bool is_powered = false;
    private float[] handle_percentages = new float[] { 0.0f, 0.0f, 0.0f, 0.0f };
    private Vector3[] final_positions = new Vector3[4]; //handle starting position (key 00)

    private List<string> ray_targets = new List<string> { "encryption_key_blue", "encryption_key_purple", "encryption_key_orange", "encryption_key_green" };

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0]);

        for (int i = 0; i < 4; i++)
        {
            //set buttons
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false)); //increase button

            //set positions
            final_positions[i] = encryption_key_levers.transform.GetChild(i).localPosition + FINAL_LEVER_DIRECTION;
        }

        hud_info.setButtons(BUTTON_LISTS[0], 7);
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

    public void initializeEncryptionKeys()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        transmitEncryptionKeysInitializationRPC(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
    }

    public int getEncryptionKey(int index)
    {
        return Mathf.FloorToInt(99.0f * handle_percentages[index]);
    }

    private void displayAdjustment(int index)
    {
        //move physical lever
        encryption_key_levers.transform.GetChild(index).localPosition = Vector3.Lerp(Vector3.zero, final_positions[index], handle_percentages[index]);

        //update bars on screen
        float tmp_pwr = handle_percentages[index];
        Color c = ReferenceAssistor.COLOR_OPTIONS[index];
        for (int i = 0; i < 16; i++)
        {
            tmp_pwr = handle_percentages[index] - (0.0625f * i);
            float a = tmp_pwr / 0.0625f;
            //do both sides
            c.a = Mathf.Max(0.04f, a);
            for (int x = 0; x < 2; x++)
            {
                encryption_key_glasses.transform.GetChild((index * 2) + 1).GetChild(x + 2).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = c;
            }
        }

        //update text
        string encryption_number = getEncryptionKey(index).ToString();
        if (encryption_number.Length == 1)
        {
            encryption_number = "0" + encryption_number;
        }
        encryption_key_glasses.transform.GetChild(index * 2).GetChild(1).GetChild(1).GetComponent<TMP_Text>().SetText(encryption_number);
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int index = ray_targets.IndexOf(current_target.name);

        int input_direction = 0;
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs) && handle_percentages[index] < 1.0f) //E to increment
        {
            input_direction += 1;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && handle_percentages[index] > 0.0f)  //Q to decrement
        {
            input_direction -= 1;
        }
        if (input_direction != 0)
        {
            if (input_direction > 0)
            {
                handle_percentages[index] = Mathf.Min(1.0f, handle_percentages[index] + dt * MOVE_SPEED);
            }
            else
            {
                handle_percentages[index] = Mathf.Max(0.0f, handle_percentages[index] - dt * MOVE_SPEED);
            }

            BUTTON_LISTS[index][0].updateInteractable(handle_percentages[index] > 0.0f);
            BUTTON_LISTS[index][1].updateInteractable(handle_percentages[index] < 1.0f);

            transmitEncryptionKeyAdjustmentRPC(index, handle_percentages[index]);
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        for (int i = 0; i < 4; i++)
        {
            //enable buttons
            BUTTON_LISTS[i][0].updateInteractable(handle_percentages[i] > 0.0f);
            BUTTON_LISTS[i][1].updateInteractable(handle_percentages[i] < 1.0f);
            //enable icons
            encryption_key_glasses.transform.GetChild(i * 2).GetChild(1).gameObject.SetActive(true);
            //enable bar displays
            encryption_key_glasses.transform.GetChild((i * 2) + 1).GetChild(2).gameObject.SetActive(true);
            encryption_key_glasses.transform.GetChild((i * 2) + 1).GetChild(3).gameObject.SetActive(true);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        for (int i = 0; i < 4; i++)
        {
            //disable buttons
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
            //disable icons
            encryption_key_glasses.transform.GetChild(i * 2).GetChild(1).gameObject.SetActive(false);
            //disable bar displays
            encryption_key_glasses.transform.GetChild((i * 2) + 1).GetChild(2).gameObject.SetActive(false);
            encryption_key_glasses.transform.GetChild((i * 2) + 1).GetChild(3).gameObject.SetActive(false);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEncryptionKeysInitializationRPC(float handle_prcnt_blue, float handle_prcnt_purple, float handle_prcnt_orange, float handle_prcnt_green)
    {
        handle_percentages[0] = handle_prcnt_blue;
        handle_percentages[1] = handle_prcnt_purple;
        handle_percentages[2] = handle_prcnt_orange;
        handle_percentages[3] = handle_prcnt_green;
        for (int i = 0; i < 4; i++)
        {
            displayAdjustment(i);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEncryptionKeyAdjustmentRPC(int index, float handle_prcnt)
    {
        handle_percentages[index] = handle_prcnt;
        displayAdjustment(index);
    }
}