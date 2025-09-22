/*
    ControlScript.cs
    - Only runs after scene is loaded in as BridgeEnvironment
    - Handles sitting down/up AND control interactions
    - Manages the HUD display for control interaction
    - Sends user inputs to control script if looking at said control and within RAYCAST_RANGE
    Contributor(s): Jake Schott
    Last Updated: 7/7/2025
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using Steamworks;

public class ControlScript : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float RAYCAST_RANGE = 1.5f;
    private static string[] POSITION_NAMES = { "PILOT", "TACTICIAN", "ENGINEER", "CAPTAIN" };

    //GAME OBJECTS
    public GameObject cursor; //the diamond in the center of the screen
    public GameObject control_info; //UI indicator that you are looking at a control
    public GameObject control_title; //title at the top of the UI trapezoid indicator
    public GameObject seat_title; //title at the top of the rounded UI seat indicator
    public GameObject buttons_panel; //contains all the buttons/dividers inside the trapezoid
    public GameObject pause_menu;
    public GameObject settings_menu;
    public GameObject controls_menu; //in the pause menu, not the trapezoid/list
    public GameObject control_script_holder; //empty GameObject that contains all the control scripts as components
    public GameObject seat_script_holder; //empty GameObject that contains the seat script manager
    public Camera my_camera; //player's camera
    private GameObject player_prefab; //corresponding "bean"

    private GameObject myPlayer = null;
    private AnimationController myAnimationController = null;
    private Animator playerAnimator = null;
    private IKController playerIK = null;

    //CLASS VARIABLES
    private HUDInfo current_info;
    private int curr_pos = -1; //0 is Pilot, 1 is Tactician, 2 is Engineer, 3 is Captain
    private bool is_sitting = false;
    private Coroutine seat_check_coroutine = null;
    private Coroutine control_check_coroutine = null;

    //SETTINGS
    private int HUD_setting = 0; //0 is Default, 1 is Minimized, 2 is Cursor Only, 3 is None
    private bool paused = false;

    //INPUT INFO
    public static List<KeyCode[]> input_options = new List<KeyCode[]>{ 
        new KeyCode[] {KeyCode.W, KeyCode.UpArrow}, //first argument is displayed, others are not
        new KeyCode[] {KeyCode.A, KeyCode.LeftArrow},
        new KeyCode[] {KeyCode.S, KeyCode.DownArrow},
        new KeyCode[] {KeyCode.D, KeyCode.RightArrow},
        new KeyCode[] {KeyCode.Q, KeyCode.LeftArrow},
        new KeyCode[] {KeyCode.E, KeyCode.RightArrow},
        new KeyCode[] {KeyCode.Mouse0, KeyCode.KeypadEnter},
        new KeyCode[] {KeyCode.Alpha1, KeyCode.Keypad1},
        new KeyCode[] {KeyCode.Alpha2, KeyCode.Keypad2},
        new KeyCode[] {KeyCode.Alpha3, KeyCode.Keypad3},
        new KeyCode[] {KeyCode.Alpha4, KeyCode.Keypad4},
        new KeyCode[] {KeyCode.F},
        new KeyCode[] {KeyCode.Z},
        new KeyCode[] {KeyCode.V}
    };

    public static bool checkInputIndex(int input_index, List<KeyCode> inputs_to_check)
    {
        for (int i = 0; i < input_options[input_index].Length; i++)
        {
            if (inputs_to_check.Contains(input_options[input_index][i]))
            {
                return true;
            }
        }
        return false;
    }

    public static ControlScript Instance { get; private set; }

    void Start()
    {
        StartCoroutine(yieldForLoad());

        //playerIK = FindAnyObjectByType<IKController>();
        //Animator[] animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject player in players)
        {
            if(player.GetComponent<PlayerMove>().IsOwner)
            {
                myPlayer = player;
                myAnimationController = player.GetComponent<AnimationController>();
                playerAnimator = myPlayer.transform.GetChild(1).GetComponent<Animator>();
                playerIK = myPlayer.transform.GetChild(1).GetComponent<IKController>();
                break;
                
            }
        }

    }

    IEnumerator yieldForLoad()
    {
        //wait to be in BridgeEnvironment scene
        while (SceneManager.GetActiveScene().name != "BridgeEnvironment")
        {
            yield return null;
        }

        //make an instance so can be referenced by CameraMove
        if (Instance != null)
        {
            Destroy(this);
        }
        Instance = this;

        //find player, set player, free camera
        string player_prefab_name = SteamClient.Name + "_" + SteamClient.SteamId.ToString();
        player_prefab = GameObject.Find(player_prefab_name);
        while (player_prefab == null)
        {
            player_prefab = GameObject.Find(player_prefab_name);
            yield return null;
        }
        player_prefab.transform.GetChild(0).GetComponent<CameraMove>().initialize();

        //wait for camera
        while (my_camera == null)
        {
            yield return null;
        }

        //begin control interfacing
        unpause();
        control_info.SetActive(false); //hide UI indicator to start
        control_script_holder = GameObject.FindWithTag("ControlHandler");
        seat_script_holder = GameObject.FindWithTag("SeatHandler");

        //free player movement, start checking to sit down, begin the scenario
        player_prefab.GetComponent<PlayerMove>().initialize();
        seat_check_coroutine = StartCoroutine(seatCheck());

        GameObject scenario_manager = GameObject.FindWithTag("ScenarioManager");
        scenario_manager.GetComponent<ScenarioManager>().initializeScenarioManager();
    }

    //used to clear buttons and minimized list entries
    private void clearButtons()
    {
        //clear trapezoid buttons
        for (int i = control_info.transform.GetChild(0).GetChild(4).childCount - 1; i >= 3; i--)
        {
            GameObject to_destroy = control_info.transform.GetChild(0).GetChild(4).GetChild(i).gameObject;
            UnityEngine.Object.Destroy(to_destroy);
        }

        //clear list entries
        for (int i = control_info.transform.GetChild(1).childCount - 1; i >= 1; i--)
        {
            GameObject to_destroy = control_info.transform.GetChild(1).GetChild(i).gameObject;
            UnityEngine.Object.Destroy(to_destroy);
        }
    }

    //used to instantiate buttons/list entries for either trapezoid or minimized list
    private void createButtons()
    {
        //hide both UI indicators
        control_info.transform.GetChild(0).gameObject.SetActive(false); //make the trapezoid invisible
        control_info.transform.GetChild(1).gameObject.SetActive(false); //make the list visible

        //get rid of existing buttons and list entries
        clearButtons();

        //if trapezoid or minimized list, then create visual buttons/list entries
        if (HUD_setting < 2)
        {
            control_info.transform.GetChild(HUD_setting).gameObject.SetActive(true);

            GameObject frame = control_info.transform.GetChild(0).gameObject;
            if (HUD_setting == 1) //if minimized list
            {
                frame = control_info.transform.GetChild(1).gameObject;
            }

            for (int i = 0; i < current_info.numOptions(); i++)
            {
                current_info.getButtons()[i].createVisual(HUD_setting, current_info.getLayout(), i, frame);
            }
        }
    }

    //used to update buttons that may no longer be interactable
    private void updateButtons(HUDInfo temp_info)
    {
        for (int b = 0; b < current_info.numOptions(); b++)
        {
            current_info.getButtons()[b].updateInteractable(temp_info.getButtons()[b].getInteractable());
        }
    }

    //used by settings
    public void setHUD(int new_hud)
    {
        if (new_hud != HUD_setting)
        {
            if (new_hud >= 0 && new_hud < 4)
            {
                HUD_setting = new_hud;
            }
            if (HUD_setting != 0)
            {
                control_info.transform.GetChild(0).gameObject.SetActive(false); //make the trapezoid invisible
                control_info.transform.GetChild(2).gameObject.SetActive(false); //make the seat rounded rectangle invisible
            }
            if (HUD_setting != 1)
            {
                control_info.transform.GetChild(1).gameObject.SetActive(false); //make the list invisible
            }
            control_title.GetComponent<TMP_Text>().SetText(""); //forces an update
        }
    }
    public bool isPaused()
    {
        return paused;
    }
    public void pause()
    {
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        pause_menu.SetActive(true);
        settings_menu.SetActive(false);
        controls_menu.SetActive(false);
        paused = true;
        cursor.SetActive(false);
    }
    public void unpause()
    {
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        pause_menu.SetActive(false);
        settings_menu.SetActive(false);
        controls_menu.SetActive(false);
        paused = false;
        if (HUD_setting != 3)
        {
            cursor.SetActive(true);
        }
    }

    public bool isSitting()
    {
        return is_sitting;
    }

    public int currentSeat()
    {
        return curr_pos;
    }

    //runs on Update() time
    IEnumerator seatCheck()
    {
        while (is_sitting == false)
        {
            yield return null;
            checkForSeats();

        }
        seat_check_coroutine = null;
    }

    private void checkForSeats()
    {
        if (!paused)
        {
            int closest_seat = seat_script_holder.GetComponent<SeatManager>().checkSeats(player_prefab.transform.position);
            if (closest_seat >= 0) //can sit
            {

                

                if (HUD_setting == 0)
                {
                    seat_title.GetComponent<TMP_Text>().SetText(POSITION_NAMES[closest_seat] + " POSITION");
                    control_info.transform.GetChild(2).gameObject.SetActive(true);
                }
                else if (HUD_setting == 1)
                {
                    control_info.transform.GetChild(1).gameObject.SetActive(true);
                }
                control_info.SetActive(true);
            }
            else //can't sit
            {
                control_info.SetActive(false);
            }
            if (UnityEngine.Input.GetKeyDown(input_options[13][0])) //trying to sit down
            {
                is_sitting = seat_script_holder.GetComponent<SeatManager>().sitDown(player_prefab.transform.position);
                if (is_sitting == true)
                {
                    curr_pos = closest_seat;

                    control_info.SetActive(false);
                    control_info.transform.GetChild(2).gameObject.SetActive(false);
                    control_info.transform.GetChild(1).gameObject.SetActive(false);
                    control_info.transform.GetChild(1).GetChild(0).gameObject.SetActive(false);

                    player_prefab.GetComponent<PlayerMove>().sitDown(curr_pos);

                    playerAnimator.SetInteger("Seat", closest_seat);
                    Vector3 playerPos = myPlayer.transform.GetChild(1).localPosition;


                    control_check_coroutine = StartCoroutine(controlCheck());
                }
            }
            return;
        }
        control_info.SetActive(false);
    }

    //runs on Update() time
    IEnumerator controlCheck()
    {
        while (is_sitting == true)
        {
            yield return null;
            checkForControlsAndInputs();
        }

        control_check_coroutine = null;
    }

    //called by controlCheck() every frame
    private void checkForControlsAndInputs()
    {
        if (my_camera != null)
        {
            if (!paused)
            {
                //check if trying to unseat
                if (UnityEngine.Input.GetKeyDown(input_options[13][0])) //trying to stand up
                {
                    //playerIK.ikActive = false;
                    playerIK.setIKRightArm(false);
                    playerIK.setIKLeftArm(false);
                    myAnimationController.setCharacterPosition(new Vector3(0, 0.16f, 0));

                    is_sitting = !seat_script_holder.GetComponent<SeatManager>().getUp(curr_pos);
                    if (is_sitting == false)
                    {
                        control_info.SetActive(false);

                        control_info.transform.GetChild(0).gameObject.SetActive(false);
                        control_info.transform.GetChild(1).gameObject.SetActive(false);
                        control_info.transform.GetChild(1).GetChild(0).gameObject.SetActive(true);
                        control_title.GetComponent<TMP_Text>().SetText("");
                        clearButtons();

                        player_prefab.GetComponent<PlayerMove>().getUp(curr_pos);

                        curr_pos = -1;
                        seat_check_coroutine = StartCoroutine(seatCheck());
                        return;
                    }
                }
                
                //else check controls
                if (Physics.Raycast(new Ray(my_camera.transform.position, my_camera.transform.forward), out RaycastHit hit, RAYCAST_RANGE)) //cast ray
                {
                    if (hit.collider.gameObject.layer == 6) //the ray hit a control (Layer 6 = Control)
                    {
                        
                        //playerIK.ikActive = true;
                        playerIK.setIKRightArm(true);
                        playerIK.setRightArmIKPosition(hit.collider.transform.position);

                        IControllable target_control =
                            (IControllable)control_script_holder.GetComponent(hit.collider.transform.GetChild(0).name); //get corresponding class

                        HUDInfo temp_info = target_control.getHUDinfo(hit.collider.gameObject);

                        if (control_title.GetComponent<TMP_Text>().text.CompareTo(temp_info.getName()) != 0 || current_info.numOptions() != temp_info.numOptions())
                        {
                            control_title.GetComponent<TMP_Text>().SetText(temp_info.getName()); //set title of that control
                            current_info = temp_info;
                            if (HUD_setting < 2) //trapezoid or minimized
                            {
                                createButtons();
                            }
                        }
                        else
                        {
                            if (HUD_setting < 2) //trapezoid or minimized
                            {
                                updateButtons(temp_info);
                            }
                        }

                        List<KeyCode> current_inputs = new List<KeyCode>(); //gets all inputted keys
                        for (int b = 0; b < current_info.numOptions(); b++)
                        {
                            Button curr_button = current_info.getButtons()[b];
                            bool pressed = false;
                            for (int i = 0; i < input_options[curr_button.getControlIndex()].Length; i++)
                            {
                                if (curr_button.getTogglable() == false)
                                {
                                    if (UnityEngine.Input.GetKey(input_options[curr_button.getControlIndex()][i]))
                                    {
                                        pressed = true;
                                    }
                                }
                                else
                                {
                                    if (UnityEngine.Input.GetKeyDown(input_options[curr_button.getControlIndex()][i]))
                                    {
                                        pressed = true;
                                    }
                                }
                                if (pressed == true)
                                {
                                    current_inputs.Add(input_options[curr_button.getControlIndex()][i]);
                                    curr_button.highlight(Time.deltaTime);
                                    break;
                                }
                            }
                            if (pressed == false)
                            {
                                curr_button.darken(Time.deltaTime);
                            }
                        }
                        control_info.SetActive(true); //show UI indicator
                        float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
                        target_control.handleInputs(current_inputs, hit.collider.gameObject, dt, curr_pos); //call when all inputs have been checked
                        return;
                    }
                }
            }
            //playerIK.ikActive = false;
            playerIK.setIKRightArm(false);
            playerIK.setIKLeftArm(false);




            control_info.SetActive(false); //hide UI indicator if not looking at a control
            control_title.GetComponent<TMP_Text>().SetText(""); //forces an update if not looking at a control
        }
    }
}
