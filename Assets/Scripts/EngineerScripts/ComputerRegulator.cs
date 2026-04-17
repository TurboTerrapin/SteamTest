/*
    ComputerRegulator.cs
    - Allows the toggling of various computer programs
    Contributor(s): Jake Schott
    Last Updated: 3/24/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ComputerRegulator : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static Vector3 BUTTON_MOVE_DIRECTION = new Vector3(0.002f, -0.004f, -0.002f);
    private static float BUTTON_PRESS_TIME = 0.12f;
    private static Color ACTIVE_COLOR = new Color(0.0f, 1.0f, 0.0f);
    private static Color INACTIVE_COLOR = new Color(1.0f, 0.0f, 0.0f);
    private static Color[] PROGRAM_COLORS = new Color[] { ReferenceAssistor.COLOR_OPTIONS[0], ReferenceAssistor.COLOR_OPTIONS[1], ReferenceAssistor.COLOR_OPTIONS[3], ReferenceAssistor.COLOR_OPTIONS[2] };

    private static string[] PROGRAM_NAMES = new string[] { "RESEARCH", "SECURITY", "DATA STORAGE", "LIFE SUPPORT" };
    private static string[][] PROGRAM_FEATURES = new string[][] { 
        new string[]{ "BIOLOGY", "GEOLOGY", "RADIATION", "LINGUISTICS", "CHEMISTRY" },
        new string[]{ "ELEVATORS", "DOORS", "CAMERAS", "VAULT", "BRIG" },
        new string[]{ "COMMUNICATIONS", "PASSKEYS", "SHIP LOGS", "CREW INFO", "NAVIGATION" },
        new string[]{ "FOOD SUPPLY", "VENTILATION", "HEATING", "WATER", "LAUNDRY" },
    };

    private string CONTROL_NAME = "COMPUTER REGULATOR";
    private static string INFO_MESSAGE = "Controls overall ship computer infrastructure to handle malfunctions or hack attempts.";
    private List<string> CONTROL_DESCS = new List<string> { "UP", "DOWN", "TOGGLE", "LEFT", "RIGHT" };
    private List<int> CONTROL_INDEXES = new List<int>() { 0, 2, 6, 1, 3 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject computer_regulator_display;
    public AudioSource computer_regulator_boop_sound;
    public List<GameObject> computer_regulator_buttons = null;

    private GameObject header;
    private GameObject bullets;
    private GameObject footer;

    private int current_page = 0; //0-3, research, security, data storage, life support
    private int current_selection = 0; //0-4, top-down bullets
    private bool[][] active_programs = new bool[][]
    {
        new bool[] {false, false, false, false, false},
        new bool[] {false, false, false, false, false},
        new bool[] {false, false, false, false, false},
        new bool[] {false, false, false, false, false},
    };

    private bool is_powered = false;
    private Coroutine button_press_coroutine = null;

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    public float lerp_speed = 5f;
    
    public int finger_position = 0;
    private int button_index = 0;

    private void Start()
    {
        header = computer_regulator_display.transform.GetChild(0).gameObject;
        bullets = computer_regulator_display.transform.GetChild(1).gameObject;
        footer = computer_regulator_display.transform.GetChild(2).gameObject;

        hud_info = new HUDInfo(CONTROL_NAME);

        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[3], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[4], CONTROL_INDEXES[4], false, true));

        hud_info.setButtons(BUTTONS, 9);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
    public Transform getIKTarget(GameObject current_target)
    {
        return IK_targets[button_index].transform;
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
    public void initializeComputerRegulator()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        int[][] temp_active_programs = new int[4][];

        for (int x = 0; x < 4; x++)
        {
            temp_active_programs[x] = new int[5];
            for (int y = 0; y < 5; y++)
            {
                int active = 0;
                if (Random.Range(0, 3) == 0)
                {
                    active = 1;
                }
                temp_active_programs[x][y] = active;
            }
        }

        transmitActiveProgramsRPC(DataConverter.arrayToString(temp_active_programs[0]),
                                  DataConverter.arrayToString(temp_active_programs[1]),
                                  DataConverter.arrayToString(temp_active_programs[2]),
                                  DataConverter.arrayToString(temp_active_programs[3]));
    }

    public void resetToDefault()
    {
        current_page = 0;
        current_selection = 0;
        displayPageAdjustment();
        displaySelectionAdjustment();
    }

    private void displayPageAdjustment()
    {
        Color page_color = PROGRAM_COLORS[current_page];

        //adjust header
        for (int i = 0; i < 4; i++)
        {
            header.transform.GetChild(0).GetChild(i).gameObject.SetActive(i == current_page);
        }
        header.transform.GetChild(1).GetComponent<TMP_Text>().SetText(PROGRAM_NAMES[current_page]);
        header.transform.GetChild(1).GetComponent<TMP_Text>().color = page_color;
        header.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().color = page_color;

        //adjust bullets
        for (int i = 0; i < 5; i++)
        {
            bullets.transform.GetChild(i).GetChild(2).GetComponent<TMP_Text>().SetText(PROGRAM_FEATURES[current_page][i]);
        }

        //adjust footer
        for (int i = 0; i < 2; i++)
        {
            footer.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = page_color;
            footer.transform.GetChild(i).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = page_color;
        }
        footer.transform.GetChild(2).GetComponent<TMP_Text>().SetText("PAGE " + (current_page + 1));
        footer.transform.GetChild(2).GetComponent<TMP_Text>().color = page_color;
    }

    private void displaySelectionAdjustment()
    {
        Color page_color = PROGRAM_COLORS[current_page];

        //adjust bullets
        for (int i = 0; i < 5; i++)
        {
            Color c = page_color;
            if (i != current_selection)
            {
                c.a = 0.2f;
            }
            bullets.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = c;
            bullets.transform.GetChild(i).GetChild(2).GetComponent<TMP_Text>().color = c;
            c = ACTIVE_COLOR;
            if (active_programs[current_page][i] == false)
            {
                c = INACTIVE_COLOR;
            }
            if (i != current_selection)
            {
                c.a = 0.2f;
            }
            bullets.transform.GetChild(i).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = c;
        }

        //adjust footer
        footer.transform.GetChild(3).GetChild(1).gameObject.SetActive(active_programs[current_page][current_selection]);
        footer.transform.GetChild(3).GetChild(3).gameObject.SetActive(!active_programs[current_page][current_selection]);
    }

    IEnumerator buttonPress(int index, int pg, int s)
    {
        button_index = index;
        for (int i = 0; i < 5; i++)
        {
            BUTTONS[i].updateInteractable(false);
            computer_regulator_buttons[i].transform.localPosition = Vector3.zero;
        }

        for (int i = 0; i < 2; i++)
        {
            float half_time = BUTTON_PRESS_TIME * 0.5f;
            float push_time = half_time;

            while (push_time > 0.0f)
            {
                push_time = Mathf.Max(0.0f, push_time - Time.deltaTime);

                float push_percentage = 1.0f - (push_time / half_time);
                if (i == 1)
                {
                    push_percentage = (push_time / half_time);
                }
                
                computer_regulator_buttons[index].transform.localPosition = Vector3.Lerp(Vector3.zero, BUTTON_MOVE_DIRECTION, push_percentage);

                yield return null;
            }

            if (i == 0)
            {
                computer_regulator_boop_sound.Play();
                if (current_page != pg)
                {
                    current_page = pg;
                    displayPageAdjustment();
                }
                displaySelectionAdjustment();
            }
        }

        for (int i = 0; i < 5; i++)
        {
            BUTTONS[i].untoggle();
            BUTTONS[i].updateInteractable(true);
        }

        button_press_coroutine = null;
        button_index = 5;
    }

    
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false || button_press_coroutine != null)
        {
            return;
        }

        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[2], inputs))
        {
            BUTTONS[2].toggle();
            BUTTONS[2].updateInteractable(false);
            transmitProgramActiveAdjustmentRPC(current_page, current_selection, !active_programs[current_page][current_selection]);
            return;
        }

        for (int i = 0; i < CONTROL_INDEXES.Count; i++)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[i], inputs) && i != 2)
            {
                int new_page = current_page;
                int new_selection = current_selection;
                if (i == 0)
                {
                    new_selection--;
                    if (new_selection < 0)
                    {
                        new_selection = 4;
                    }
                }
                else if (i == 1)
                {
                    new_selection++;
                    if (new_selection > 4)
                    {
                        new_selection = 0;
                    }
                }
                else if (i == 3)
                {
                    new_page--;
                    if (new_page < 0)
                    {
                        new_page = 3;
                    }
                }
                else
                {
                    new_page++;
                    if (new_page > 3)
                    {
                        new_page = 0;
                    }
                }

                BUTTONS[i].toggle();
                BUTTONS[i].updateInteractable(false);
                transmitSelectionAdjustmentRPC(i, new_page, new_selection);
                return;
            }
        }
    }


    public void powerOn(int position)
    {
        is_powered = true;

        computer_regulator_display.SetActive(true);

        for (int i = 0; i < 5; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;

        computer_regulator_display.SetActive(false);

        for (int i = 0; i < 5; i++)
        {
            BUTTONS[i].updateInteractable(false);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitActiveProgramsRPC(string r, string s, string ds, string ls)
    {
        int[][] programs_to_update = new int[4][];
        programs_to_update[0] = DataConverter.stringToArray(r); //research
        programs_to_update[1] = DataConverter.stringToArray(s); //security
        programs_to_update[2] = DataConverter.stringToArray(ds); //data storage
        programs_to_update[3] = DataConverter.stringToArray(ls); //life support

        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                active_programs[x][y] = (programs_to_update[x][y] == 1);
            }
        }

        displayPageAdjustment();
        displaySelectionAdjustment();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitProgramActiveAdjustmentRPC(int pg, int s, bool active)
    {
        active_programs[pg][s] = active;
        if (button_press_coroutine != null)
        {
            StopCoroutine(button_press_coroutine);
        }
        button_press_coroutine = StartCoroutine(buttonPress(2, pg, s));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSelectionAdjustmentRPC(int index, int pg, int s)
    {
        current_selection = s;
        if (button_press_coroutine != null)
        {
            StopCoroutine(button_press_coroutine);
        }
        button_press_coroutine = StartCoroutine(buttonPress(index, pg, s));
    }
}