/*
    ControlScript.cs
    - Only runs after scene is loaded in as BridgeEnvironment
    - Handles sitting down/up AND control interactions
    - Manages the HUD display for control interaction
    - Sends user inputs to control script if looking at said control and within RAYCAST_RANGE
    - Handles transmitting IK targets for hand movement animations
    Contributor(s): Jake Schott, John Aylward
    Last Updated: 1/4/2026
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ControlScript : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float RAYCAST_RANGE = 1.5f;
    private static string[] POSITION_NAMES = { "PILOT", "TACTICIAN", "ENGINEER", "CAPTAIN" };

    //GAME OBJECTS
    public GameObject cursor; //the diamond in the center of the screen
    public GameObject control_info; //UI indicator that you are looking at a control
    public GameObject secondary_info; //UI indicators on the left and right sides of the screen (only visible in default mode)
    public GameObject control_title; //title at the top of the UI trapezoid indicator
    public GameObject seat_title; //title at the top of the rounded UI seat indicator
    public GameObject buttons_panel; //contains all the buttons/dividers inside the trapezoid
    public GameObject pause_menu;
    public GameObject settings_menu;
    public GameObject controls_menu; //in the pause menu, not the trapezoid/list
    public GameObject control_script_holder; //empty GameObject that contains all the control scripts as components
    public GameObject sensor_script_holder; //empty GameObject that contains all the sensor scripts as components
    public SeatManager seat_manager; //empty GameObject that contains the seat script manager
    private Camera plr_camera; //player's camera
    private GameObject player_prefab; //corresponding "bean"
    private AnimationController my_animation_controller = null;

    //CLASS VARIABLES
    private HUDInfo current_info;
    private GameObject current_ray_target = null;
    private int curr_pos = -1; //0 is Pilot, 1 is Tactician, 2 is Engineer, 3 is Captain
    private float displayed_power = 0.0f; //used for the power indicator in the bottom right (5 circles)
    private bool is_sitting = false;
    private Coroutine seat_check_coroutine = null;
    private Coroutine control_check_coroutine = null;
    private Coroutine ray_target_check_coroutine = null;

    //SETTINGS
    private int HUD_setting = 0; //0 is Default, 1 is Trapezoid Only, 2 is Minimized, 3 is Cursor Only, 4 is None
    private bool can_pause = false;
    private bool paused = false;
    private bool is_active = false;

    //INPUT INFO
    public static List<KeyCode[]> input_options = new List<KeyCode[]>{
        new KeyCode[] {KeyCode.W, KeyCode.UpArrow}, //first argument is displayed, others are not
        new KeyCode[] {KeyCode.A, KeyCode.LeftArrow},
        new KeyCode[] {KeyCode.S, KeyCode.DownArrow},
        new KeyCode[] {KeyCode.D, KeyCode.RightArrow},
        new KeyCode[] {KeyCode.Q, KeyCode.LeftArrow},
        new KeyCode[] {KeyCode.E, KeyCode.RightArrow},
        new KeyCode[] {KeyCode.Mouse0, KeyCode.KeypadEnter, KeyCode.Return},
        new KeyCode[] {KeyCode.Alpha1, KeyCode.Keypad1},
        new KeyCode[] {KeyCode.Alpha2, KeyCode.Keypad2},
        new KeyCode[] {KeyCode.Alpha3, KeyCode.Keypad3},
        new KeyCode[] {KeyCode.Alpha4, KeyCode.Keypad4},
        new KeyCode[] {KeyCode.F},
        new KeyCode[] {KeyCode.Z},
        new KeyCode[] {KeyCode.Space},
        new KeyCode[] {KeyCode.LeftShift, KeyCode.RightShift},
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
        //make an instance so can be referenced
        if (Instance != null)
        {
            Destroy(this);
        }
        Instance = this;
    }

    public void unlockPlayer(GameObject plr_prefab)
    {
        player_prefab = plr_prefab;

        my_animation_controller = plr_prefab.GetComponent<AnimationController>();

        plr_camera = player_prefab.transform.GetComponent<CameraMove>().camera_transform.GetComponent<Camera>();
        player_prefab.transform.GetComponent<CameraMove>().initialize();

        //begin control interfacing
        unpause();
        control_info.SetActive(false); //hide UI indicator to start
        control_script_holder = GameObject.FindWithTag("ControlHandler");
        sensor_script_holder = GameObject.FindGameObjectWithTag("SensorHandler");
        seat_manager = GameObject.FindWithTag("SeatHandler").GetComponent<SeatManager>();

        //free player movement, start checking to sit down, begin the scenario
        is_active = true;
        can_pause = true;
        player_prefab.GetComponent<PlayerMove>().initialize();
        seat_check_coroutine = StartCoroutine(seatCheck());
    }

    //used to clear buttons and minimized list entries
    private void clearButtons()
    {
        //clear trapezoid buttons
        for (int i = control_info.transform.GetChild(0).GetChild(4).childCount - 1; i >= 2; i--)
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
    private void initializeControlInfo()
    {
        //hide both UI indicators
        control_info.transform.GetChild(0).gameObject.SetActive(false); //make the trapezoid invisible
        control_info.transform.GetChild(1).gameObject.SetActive(false); //make the list visible

        //get rid of existing buttons and list entries
        clearButtons();

        //if trapezoid or minimized list, then create visual buttons/list entries
        if (HUD_setting < 3)
        {
            control_info.transform.GetChild(0).gameObject.SetActive(HUD_setting < 2);
            control_info.transform.GetChild(1).gameObject.SetActive(HUD_setting == 2);

            GameObject frame = control_info.transform.GetChild(0).gameObject; //trapezoid
            if (HUD_setting == 2) //if minimized list
            {
                frame = control_info.transform.GetChild(1).gameObject;
            }

            for (int i = 0; i < current_info.numOptions(); i++)
            {
                current_info.getButtons()[i].createVisual(HUD_setting, current_info.getLayout(), i, frame);
            }

            //if no buttons, apply descriptor using HUDInfo
            if (current_info.numOptions() == 0)
            {
                if (HUD_setting < 2)
                {
                    current_info.applyDescriptor(frame.transform);
                }
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
            if (new_hud >= 0 && new_hud < 5)
            {
                HUD_setting = new_hud;
            }
            control_info.transform.GetChild(0).gameObject.SetActive(HUD_setting < 2 && is_sitting == true); //trapezoid
            control_info.transform.GetChild(1).gameObject.SetActive(HUD_setting == 2); //minimized list
            control_info.transform.GetChild(2).gameObject.SetActive(HUD_setting < 2 && is_sitting == false); //rounded seat indicator
            control_title.GetComponent<TMP_Text>().SetText(""); //forces an update
        }
    }

    public bool isPaused()
    {
        return paused;
    }

    public bool canPause()
    {
        return can_pause;
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
        if (is_active == true)
        {
            if (HUD_setting != 4)
            {
                cursor.SetActive(true);
            }
        }
    }

    public void deactivate(bool allow_pausing, bool free_cursor)
    {
        is_active = false;
        can_pause = allow_pausing;
        if (allow_pausing == false && paused == true)
        {
            unpause();
        }
        if (free_cursor == true)
        {
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
        cursor.SetActive(false);
    }

    public void reactivate()
    {
        is_active = true;
        can_pause = true;
        if (paused == false)
        {
            unpause();
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

    //called by seatCheck()
    private void checkForSeats()
    {
        if (!paused && is_active)
        {
            int closest_seat = seat_manager.checkSeats(player_prefab.transform.position);
            if (closest_seat >= 0) //can sit
            {
                control_info.transform.GetChild(1).gameObject.SetActive(HUD_setting == 2);
                control_info.transform.GetChild(2).gameObject.SetActive(HUD_setting < 2);
                seat_title.GetComponent<TMP_Text>().SetText(POSITION_NAMES[closest_seat] + " POSITION");
                control_info.SetActive(true);

                if (UnityEngine.Input.GetKeyDown(input_options[13][0])) //trying to sit down
                {
                    is_sitting = seat_manager.sitDown(closest_seat);
                    if (is_sitting == true)
                    {
                        curr_pos = closest_seat;
                        control_info.SetActive(false);
                        player_prefab.GetComponent<CameraMove>().lockCamera();
                        player_prefab.GetComponent<CameraMove>().camera_transform.parent = player_prefab.GetComponent<CameraMove>().head_transform;
                        player_prefab.GetComponent<PlayerMove>().sitDown(curr_pos);
                    }
                }
            }
            else //can't sit
            {
                control_info.SetActive(false);
            }

            return;
        }
        control_info.SetActive(false);
    }

    //updates shift direction UI indicator and get up indicator
    public void updateShiftIndicators()
    {
        bool shifting = player_prefab.transform.GetComponent<PlayerMove>().isShifting();
        secondary_info.transform.GetChild(0).GetChild(2).gameObject.SetActive(curr_pos != 3);
        secondary_info.transform.GetChild(0).GetChild(2).GetChild(2).GetChild(0).gameObject.SetActive(seat_manager.canShiftLeft(curr_pos) && !shifting);
        secondary_info.transform.GetChild(0).GetChild(2).GetChild(3).GetChild(0).gameObject.SetActive(seat_manager.canShiftRight(curr_pos) && !shifting);
        secondary_info.transform.GetChild(0).GetChild(2).GetChild(4).GetChild(0).gameObject.SetActive(!shifting);
        secondary_info.transform.GetChild(0).GetChild(1).GetChild(2).GetChild(0).gameObject.SetActive(!shifting);
        secondary_info.transform.GetChild(0).GetChild(1).GetChild(3).GetChild(0).gameObject.SetActive(!shifting);
    }

    //called by AnimatorHandler when sit down animation is completed
    public void assumePosition()
    {
        player_prefab.GetComponent<CameraMove>().parentRotationLock = true;
        player_prefab.GetComponent<CameraMove>().captainMode = (curr_pos == 3);
        if (curr_pos != 3)
        {
            player_prefab.GetComponent<CameraMove>().unlockCamera(new Vector2(0.0f, 30.0f));
        }
        else
        {
            player_prefab.GetComponent<CameraMove>().unlockCamera(new Vector2(180.0f, 30.0f)); //captain is flipped for some reason
        }
        my_animation_controller.setIKActive(true);
        my_animation_controller.setIKHead(true);

        secondary_info.SetActive(HUD_setting == 0);
        updateShiftIndicators();
        secondary_info.transform.GetChild(1).gameObject.SetActive(false);

        control_info.transform.GetChild(0).gameObject.SetActive(HUD_setting < 2);
        control_info.transform.GetChild(1).gameObject.SetActive(HUD_setting == 2);
        control_info.transform.GetChild(2).gameObject.SetActive(false);
        control_info.transform.GetChild(1).GetChild(0).gameObject.SetActive(false);

        ray_target_check_coroutine = StartCoroutine(rayCheck());
        control_check_coroutine = StartCoroutine(controlCheck());
    }

    //called by AnimatorHandler when get up animation is completed
    public void relinquishPosition()
    {
        player_prefab.GetComponent<CameraMove>().parentRotationLock = false;
        float[] rotations = new float[] { 0.0f, 0.0f, 135.0f, 180.0f };
        player_prefab.GetComponent<CameraMove>().unlockCamera(new Vector2(rotations[curr_pos], 30.0f));
        player_prefab.GetComponent<CameraMove>().captainMode = false;
        my_animation_controller.setIKActive(true);
        my_animation_controller.setIKHead(true);
        my_animation_controller.setIKLeftArm(false);
        my_animation_controller.setIKRightArm(false);

        player_prefab.transform.position = player_prefab.transform.Find("Character").position - new Vector3(0.0f, 0.12f, 0.0f);
        player_prefab.GetComponent<PlayerMove>().initialize();

        seat_manager.getUp(curr_pos);

        curr_pos = -1;
        seat_check_coroutine = StartCoroutine(seatCheck());
    }

    //helper method that esimates the length of a control description based on the length of the description of that control's description
    private int getControlInfoOffset(HUDInfo temp_info)
    {
        return Mathf.Max(100, temp_info.getInfo().Length * 4);
    }

    //runs on FixedUpdate() time (this code is meant to improve raycast consistency/avoid flickering)
    IEnumerator rayCheck()
    {
        float cooldown = 0.0f;
        current_ray_target = null;
        while (true)
        {
            if (plr_camera != null && is_active == true)
            {
                if (Physics.Raycast(new Ray(plr_camera.transform.position, plr_camera.transform.forward), out RaycastHit hit, RAYCAST_RANGE, LayerMask.GetMask("RayTarget")))
                {
                    current_ray_target = hit.collider.gameObject;
                    cooldown = 0.0f;
                }
                else
                {
                    cooldown += Time.fixedDeltaTime;
                    if (cooldown >= 0.12f)
                    {
                        current_ray_target = null;
                        cooldown = 0.0f;
                    }
                }
            }
            else
            {
                cooldown = 0.0f;
                current_ray_target = null;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    //runs on Update() time
    IEnumerator controlCheck()
    {
        while (is_sitting == true)
        {
            yield return null;
            checkForControlsAndInputs();
        }

        StopCoroutine(ray_target_check_coroutine);
        control_check_coroutine = null;
        ray_target_check_coroutine = null;
    }

    //called by controlCheck() every frame, checks if trying to unsit/shift then checks for RayTargets
    private void checkForControlsAndInputs()
    {
        if (plr_camera != null)
        {
            if (!paused && is_active)
            {
                //-----------------------------------------------CHECK FOR UNSEATING/SHIFTING--------------------------------------------------
                if (player_prefab.GetComponent<PlayerMove>().isShifting() == false)
                {
                    //check if trying to unseat
                    if (UnityEngine.Input.GetKeyDown(input_options[13][0])) //trying to stand up
                    {
                        is_sitting = false;

                        my_animation_controller.setIKActive(false);

                        control_info.SetActive(false);
                        secondary_info.SetActive(false);

                        control_info.transform.GetChild(0).gameObject.SetActive(false);
                        control_info.transform.GetChild(1).gameObject.SetActive(false);
                        control_info.transform.GetChild(1).GetChild(0).gameObject.SetActive(true);
                        control_title.GetComponent<TMP_Text>().SetText("");
                        clearButtons();

                        player_prefab.GetComponent<PlayerMove>().getUp(curr_pos);

                        return;
                    }

                    //check if trying to shift
                    if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift) || UnityEngine.Input.GetKeyDown(KeyCode.RightShift)) //trying to shift
                    {
                        player_prefab.GetComponent<PlayerMove>().seatShift(curr_pos);
                    }
                }

                //----------------------------------------------------CHECK FOR RAYTARGETS------------------------------------------------------
                if (current_ray_target != null) //check if raycast hit something
                {
                    if (current_ray_target.layer == 6) //the ray hit a control or sensor descriptor (Layer 6 = RayTarget)
                    {
                        //---------------------------------------------------HANDLE UI----------------------------------------------------------
                        IControllable target_control = control_script_holder.GetComponent(current_ray_target.transform.GetChild(0).name) as IControllable;

                        HUDInfo temp_info = null;

                        if (target_control != null) //IControllable
                        {
                            temp_info = target_control.getHUDinfo(current_ray_target.gameObject);
                        }
                        else //IDescribable
                        {
                            IDescribable target_descriptor = sensor_script_holder.GetComponent(current_ray_target.transform.GetChild(0).name) as IDescribable;
                            temp_info = target_descriptor.getHUDinfo(current_ray_target.gameObject);
                        }

                        //check if current HUDInfo is different from RayTarget HUDInfo
                        if (control_title.GetComponent<TMP_Text>().text.CompareTo(temp_info.getName()) != 0 || current_info.numOptions() != temp_info.numOptions())
                        {
                            control_title.GetComponent<TMP_Text>().SetText(temp_info.getName()); //set title of that control
                            current_info = temp_info;
                            if (HUD_setting < 3) //trapezoid or minimized
                            {
                                initializeControlInfo();
                            }

                            //determine whether to show or hide the power indicator
                            secondary_info.transform.GetChild(1).GetChild(0).gameObject.SetActive(temp_info.getConsumesPower());

                            //set info frame title and description
                            secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(3).GetComponent<TMP_Text>().SetText(temp_info.getName());
                            secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(5).GetComponent<TMP_Text>().SetText(temp_info.getInfo());

                            //resize based on length of control description
                            int offset = getControlInfoOffset(temp_info);
                            secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(5).GetComponent<RectTransform>().sizeDelta = new Vector2(535f, offset);
                            secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(5).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -322f + (offset / 2));
                            secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(4).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -284f + offset);
                            secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(4).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -284f + offset);
                            secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(3).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -145f + offset);
                            secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -230f + (offset / 2));
                            secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 365f + offset);
                            secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -23f + offset);
                            if (secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).gameObject.activeSelf == true)
                            {
                                secondary_info.transform.GetChild(1).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(1595f, -530f + offset);
                            }
                        }
                        else
                        {
                            if (HUD_setting < 3) //trapezoid or minimized
                            {
                                updateButtons(temp_info);
                            }
                        }

                        //handle info showing/hiding
                        secondary_info.transform.GetChild(1).gameObject.SetActive(temp_info.hasInfo());
                        if (temp_info.hasInfo() == true)
                        {
                            //check if trying to show/hide info with tab key
                            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab) && HUD_setting == 0)
                            {
                                bool currently_visible = secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).gameObject.activeSelf;
                                secondary_info.transform.GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(!currently_visible);
                                secondary_info.transform.GetChild(1).GetChild(1).GetChild(1).gameObject.SetActive(currently_visible);

                                if (currently_visible == true)
                                {
                                    secondary_info.transform.GetChild(1).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(1595f, -890f);
                                }
                                else
                                {
                                    secondary_info.transform.GetChild(1).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(1595f, -530f + getControlInfoOffset(temp_info));
                                }
                            }
                        }

                        //check if need to update the blue power dots in the bottom right for power-consuming controls
                        if (temp_info.getConsumesPower() == true && temp_info.getPowerConsumption() != displayed_power)
                        {
                            float tmp_pwr = (temp_info.getPowerConsumption() * 2.0f);
                            for (int i = 0; i <= 4; i++)
                            {
                                tmp_pwr = (temp_info.getPowerConsumption() * 2.0f) - (0.2f * i);
                                float a = tmp_pwr / 0.2f;
                                secondary_info.transform.GetChild(1).GetChild(0).GetChild(4).GetChild(i).GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = a;
                            }
                            displayed_power = temp_info.getPowerConsumption();
                        }

                        //---------------------------------------------------HANDLE IK----------------------------------------------------------
                        if (temp_info.numOptions() > 0) //IControllable, move hand
                        {
                            IIKTargetable target_IK = control_script_holder.GetComponent(current_ray_target.transform.GetChild(0).name) as IIKTargetable; //get corresponding class
                                                                                                                                                          //if the ray target has a specific IK target, then use the IK target
                            if (target_IK != null)
                            {
                                //Debug.Log(Vector3.SignedAngle(player_prefab.transform.forward, plr_camera.transform.forward, player_prefab.transform.up));
                                if (Vector3.SignedAngle(player_prefab.transform.forward, plr_camera.transform.forward, player_prefab.transform.up) > 0)
                                //if (Vector3.SignedAngle(seat_script_holder.GetComponent<SeatManager>().physical_seats[curr_pos].transform.GetChild(2).forward, plr_camera.transform.forward, Vector3.up) > 0)
                                {
                                    //turn IK on and move the right arm target
                                    my_animation_controller.setIKRightArm(true);
                                    //Vector3 pos = target_IK.getIKTarget().position;
                                    my_animation_controller.setRightArmIKPosition(target_IK.getIKTarget().position);
                                    my_animation_controller.setRightArmIKRotation(target_IK.getIKTarget().rotation);
                                }
                                else
                                {
                                    //turn IK on and move the left arm target
                                    my_animation_controller.setIKLeftArm(true);
                                    my_animation_controller.setLeftArmIKPosition(target_IK.getIKTarget().position);
                                    my_animation_controller.setLeftArmIKRotation(target_IK.getIKTarget().rotation);
                                }
                            }
                            //otherwise fallback to normal IK mode
                            else
                            {
                                if (Vector3.SignedAngle(player_prefab.transform.forward, plr_camera.transform.forward, player_prefab.transform.up) > 0)
                                //if (Vector3.SignedAngle(seat_script_holder.GetComponent<SeatManager>().physical_seats[curr_pos].transform.GetChild(2).forward, plr_camera.transform.forward, Vector3.up) > 0)
                                {
                                    //turn IK on and move the right arm target
                                    my_animation_controller.setIKRightArm(true);
                                    my_animation_controller.setRightArmIKPosition(current_ray_target.transform.position);
                                    my_animation_controller.setIKLeftArm(false);
                                }
                                else
                                {
                                    //turn IK on and move the left arm target
                                    my_animation_controller.setIKLeftArm(true);
                                    my_animation_controller.setLeftArmIKPosition(current_ray_target.transform.position);
                                    my_animation_controller.setIKRightArm(false);
                                }
                            }
                        }
                        else //IDescribable, turn IK off
                        {
                            my_animation_controller.setIKRightArm(false);
                            my_animation_controller.setIKLeftArm(false);
                        }

                        //---------------------------------------------------HANDLE INPUTS----------------------------------------------------------
                        List<KeyCode> current_inputs = new List<KeyCode>(); //get all inputted keys
                        for (int b = 0; b < current_info.numOptions(); b++)
                        {
                            Button curr_button = current_info.getButtons()[b];
                            bool pressed = false;
                            for (int i = 0; i < input_options[curr_button.getControlIndex()].Length; i++)
                            {
                                if (curr_button.getTogglable() == false)
                                {
                                    if (UnityEngine.Input.GetKey(input_options[curr_button.getControlIndex()][i])) //GetKey
                                    {
                                        pressed = true;
                                    }
                                }
                                else
                                {
                                    if (UnityEngine.Input.GetKeyDown(input_options[curr_button.getControlIndex()][i])) //GetKeyDown
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

                        //-------------------------------------------FINAL ADJUSTMENTS--------------------------------------------------------------
                        control_info.SetActive(true); //show UI indicator
                        secondary_info.SetActive(HUD_setting == 0); //show secondary UI if default view
                        float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
                        if (target_control != null)
                        {
                            target_control.handleInputs(current_inputs, current_ray_target, dt, curr_pos); //call when all inputs have been checked
                        }
                        return;
                    }
                }
            }
            my_animation_controller.setIKRightArm(false);
            my_animation_controller.setIKLeftArm(false);

            secondary_info.SetActive(is_active == true && paused == false && HUD_setting == 0);
            secondary_info.transform.GetChild(1).gameObject.SetActive(false);
            control_info.SetActive(false); //hide UI indicator if not looking at a control
            control_title.GetComponent<TMP_Text>().SetText(""); //forces an update if not looking at a control
        }
    }
}
