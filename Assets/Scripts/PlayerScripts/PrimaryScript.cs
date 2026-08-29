/*
    PrimaryScript.cs
    - Only runs after scene is loaded
    - Handles sitting down/up AND control interactions
    - Manages the HUD display for control interaction
    - Sends user inputs to control script if looking at said control and within RAYCAST_RANGE
    - Handles transmitting IK targets for hand movement animations
    Contributor(s): Jake Schott, John Aylward
    Last Updated: 8/28/2026
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class PrimaryScript : MonoBehaviour
{
    //CLASS CONSTANTS
    protected static float RAYCAST_RANGE = 0.85f;

    //GAME OBJECTS
    public Sprite button_rounded_edge;
    protected GameObject player_UI_canvas;
    protected GameObject cursor;
    protected GameObject primary_info;
    protected GameObject default_view;
    protected GameObject minimized_view;

    protected GameObject pause_default_menu;
    protected GameObject pause_controls_menu;
    protected GameObject pause_settings_menu;
    protected GameObject pause_confirm_quit_menu;

    protected Camera plr_camera; //player's camera
    protected GameObject player_prefab; //corresponding "bean"

    protected AnimationController my_animation_controller = null;

    //CLASS VARIABLES
    protected HUDInfo current_info;
    protected GameObject current_ray_target = null;
    protected IControllable current_controllable = null;
    protected IDescribable current_describable = null;
    protected bool control_update_flag = false;
    protected bool is_sitting = false;
    protected Coroutine intro_yield_coroutine = null;
    protected Coroutine seat_check_coroutine = null;
    protected Coroutine control_check_coroutine = null;
    protected Coroutine ray_target_check_coroutine = null;

    //SETTINGS
    protected int HUD_setting = 0; //0 is Default, 1 is Essential, 2 is Minimized, 3 is Cursor Only, 4 is None
    protected bool hints_setting = false; //only applies for HUD_setting 0 and 1 (top left/right elements)
    protected bool can_pause = false;
    protected bool paused = false;
    protected bool is_active = false;

    //INPUT INFO
    public static List<KeyCode[]> input_options = new List<KeyCode[]>{
        new KeyCode[] {KeyCode.W, KeyCode.UpArrow}, //0 (first argument is displayed, others are not(
        new KeyCode[] {KeyCode.A, KeyCode.LeftArrow}, //1
        new KeyCode[] {KeyCode.S, KeyCode.DownArrow}, //2
        new KeyCode[] {KeyCode.D, KeyCode.RightArrow}, //3
        new KeyCode[] {KeyCode.Q, KeyCode.LeftArrow}, //4
        new KeyCode[] {KeyCode.E, KeyCode.RightArrow}, //5
        new KeyCode[] {KeyCode.Mouse0, KeyCode.KeypadEnter, KeyCode.Return}, //6
        new KeyCode[] {KeyCode.Alpha1, KeyCode.Keypad1}, //7
        new KeyCode[] {KeyCode.Alpha2, KeyCode.Keypad2}, //8
        new KeyCode[] {KeyCode.Alpha3, KeyCode.Keypad3}, //9
        new KeyCode[] {KeyCode.Alpha4, KeyCode.Keypad4}, //10
        new KeyCode[] {KeyCode.F}, //11
        new KeyCode[] {KeyCode.Z}, //12
        new KeyCode[] {KeyCode.Space}, //13
        new KeyCode[] {KeyCode.LeftShift, KeyCode.RightShift}, //14
        new KeyCode[] {KeyCode.T} //15
    };

    public static bool checkInputIndex(int input_index, List<KeyCode> inputs_to_check)
    {
        for (int i = 0; i < input_options[input_index].Length; i++)
        {
            if (inputs_to_check.Contains(input_options[input_index][i]) == true)
            {
                return true;
            }
        }
        return false;
    }

    public static bool checkInputIndexDown(int input_index)
    {
        for (int i = 0; i < input_options[input_index].Length; i++)
        {
            if (Input.GetKeyDown(input_options[input_index][i]) == true)
            {
                return true;
            }
        }
        return false;
    }

    public static PrimaryScript Instance { get; protected set; }

    protected void Awake()
    {
        player_UI_canvas = gameObject;
        cursor = player_UI_canvas.transform.GetChild(0).gameObject;
        primary_info = player_UI_canvas.transform.GetChild(1).gameObject;
        default_view = primary_info.transform.GetChild(0).gameObject;
        minimized_view = primary_info.transform.GetChild(1).gameObject;
        pause_default_menu = player_UI_canvas.transform.Find("Pause").GetChild(0).gameObject;
        pause_settings_menu = pause_default_menu.transform.parent.GetChild(1).gameObject;
        pause_controls_menu = pause_default_menu.transform.parent.GetChild(2).gameObject;
        pause_confirm_quit_menu = pause_default_menu.transform.parent.GetChild(3).gameObject;

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

        plr_camera = player_prefab.GetComponent<CameraMove>().cameraHolder.transform.GetChild(0).GetComponent<Camera>();
        player_prefab.GetComponent<CameraMove>().SetMouseSensitvity(player_UI_canvas.GetComponent<UniversalSettingsController>().GetCameraSensitivity());
        player_prefab.GetComponent<CameraMove>().Initialize();

        //begin control interfacing
        primary_info.SetActive(false);

        //free player movement, start checking to sit down, begin the scenario
        can_pause = true;
        player_prefab.GetComponent<PlayerMove>().Initialize();
        if (hints_setting == true && HUD_setting < 2)
        {
            GetComponent<SecondaryScript>().displayMissionObjective(this.GetType().Name.CompareTo("BridgePrimaryScript") != 0, 1.0f);
            intro_yield_coroutine = StartCoroutine(introYield());
        }
        else
        {
            onIntroComplete();
            unpause();
        }
    }

    //called after intro 
    protected void onIntroComplete()
    {
        GetComponent<SecondaryScript>().endMissionObjectiveReveal();
        GetComponent<SecondaryScript>().setPermanentOverlayVisibility(hints_setting && HUD_setting < 2);
        activate();
        seat_check_coroutine = StartCoroutine(seatCheck());
    }

    protected IEnumerator introYield()
    {
        do
        {
            yield return null;
        }
        while (GetComponent<SecondaryScript>().isDisplayingIntro() == true);

        while (Input.GetKeyDown(KeyCode.Space) == false)
        {
            yield return null;
        }

        intro_yield_coroutine = null;

        onIntroComplete();
        unpause();
    }

    //used to clear default buttons and minimized list entries
    protected void resetButtons()
    {
        //hide default buttons
        foreach (Transform t in default_view.transform.GetChild(1).GetChild(4))
        {
            t.gameObject.SetActive(false);
            t.transform.GetChild(2).GetComponent<UnityEngine.UI.Image>().sprite = button_rounded_edge;
            t.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().sprite = button_rounded_edge;
        }

        //hide default dividers
        foreach (Transform t in default_view.transform.GetChild(1).GetChild(5))
        {
            t.gameObject.SetActive(false);
        }

        //hide minimized list entries
        foreach (Transform t in minimized_view.transform.GetChild(1))
        {
            t.gameObject.SetActive(false);
        }
    }

    //used to instantiate buttons/list entries for either default view or minimized list
    protected void initializePrimaryInfo()
    {
        //hide existing buttons and list entries
        resetButtons();

        //if default or minimized view, then set visual buttons/list entries
        if (HUD_setting < 3)
        {
            default_view.SetActive(HUD_setting < 2); //default
            minimized_view.SetActive(HUD_setting == 2); //minimized

            GameObject frame = default_view.transform.GetChild(1).gameObject;
            if (HUD_setting == 2) //if minimized list
            {
                frame = minimized_view.transform.GetChild(1).gameObject;
            }

            //initialize background/title/border visual if default or essential view
            if (HUD_setting < 2)
            {
                current_info.initializeDefaultFrame(frame.transform);
            }

            //initialize button visuals
            for (int i = 0; i < current_info.numOptions(); i++)
            {
                current_info.getButtons()[i].updateVisibility(true);
                current_info.getButtons()[i].createVisual(HUD_setting, current_info.getLayout(), i, frame);
            }
        }
    }

    //used to update buttons that may no longer be interactable
    protected void updateButtons(HUDInfo temp_info)
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
            default_view.SetActive(HUD_setting < 2); //default
            minimized_view.SetActive(HUD_setting == 2); //minimized
            control_update_flag = true; //forces an update
            GetComponent<SecondaryScript>().setPermanentOverlayVisibility(HUD_setting == 0);
        }
    }

    //used by settings
    public void setCameraSensitivity(float sensitivity)
    {
        if (player_prefab != null)
        {
            player_prefab.GetComponent<CameraMove>().SetMouseSensitvity(sensitivity);
        }
    }

    //used by settings
    public void setHintsEnabled(bool enabled)
    {
        hints_setting = enabled;
        ReferenceAssistor.Instance.hints_manager.hints_overlay.SetActive(hints_setting);
    }

    public void setCursorVisibility(bool visibility)
    {
        cursor.SetActive(visibility);
    }

    protected void updateCursorMode()
    {
        //update cursor mode (either default or manual cursor)
        cursor.transform.GetChild(0).gameObject.SetActive(current_ray_target == null || !current_ray_target.name.Contains("manual_options"));
        cursor.transform.GetChild(1).gameObject.SetActive(current_ray_target != null && current_ray_target.name.Contains("manual_options"));
    }

    protected void updateCursorMode(bool default_active)
    {
        cursor.transform.GetChild(0).gameObject.SetActive(default_active);
        cursor.transform.GetChild(1).gameObject.SetActive(!default_active);
    }

    public bool isActive()
    {
        return is_active;
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
        pause_default_menu.SetActive(true);
        pause_settings_menu.SetActive(false);
        pause_controls_menu.SetActive(false);
        GetComponent<SecondaryScript>().checkStationFunctionsInput(true);
        GetComponent<SecondaryScript>().setSecondaryInfoVisibility(false);
        paused = true;
        cursor.SetActive(false);
        if (intro_yield_coroutine != null)
        {
            StopCoroutine(intro_yield_coroutine);
            intro_yield_coroutine = null;
            GetComponent<SecondaryScript>().endMissionObjectiveReveal();
            onIntroComplete();
        }
    }

    public void unpause()
    {
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        pause_default_menu.SetActive(false);
        pause_settings_menu.SetActive(false);
        pause_controls_menu.SetActive(false);
        pause_confirm_quit_menu.SetActive(false);
        GetComponent<SecondaryScript>().setSecondaryInfoVisibility(is_active && HUD_setting < 2);
        GetComponent<SecondaryScript>().setPermanentOverlayVisibility(is_active && HUD_setting == 0);
        GetComponent<SecondaryScript>().setSittingOverlayVisibility(is_active && is_sitting && HUD_setting == 0);
        paused = false;
        if (is_active == true)
        {
            if (HUD_setting != 4)
            {
                cursor.SetActive(true);
            }
        }
    }

    public void activate()
    {
        if (intro_yield_coroutine != null)
        {
            return;
        }
        is_active = true;
        can_pause = true;
        if (paused == false)
        {
            unpause();
        }
    }

    public void deactivate(bool allow_pausing, bool free_cursor)
    {
        is_active = false;
        can_pause = allow_pausing;
        GetComponent<SecondaryScript>().setSecondaryInfoVisibility(false);
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

    public int getHUD()
    {
        return HUD_setting;
    }

    public bool hintsEnabled()
    {
        return hints_setting;
    }

    public bool isSitting()
    {
        return is_sitting;
    }

    protected void onSittingChange()
    {
        default_view.transform.GetChild(0).gameObject.SetActive(!is_sitting);
        default_view.transform.GetChild(1).gameObject.SetActive(is_sitting);
        minimized_view.transform.GetChild(0).gameObject.SetActive(!is_sitting);
        minimized_view.transform.GetChild(1).gameObject.SetActive(is_sitting);
    }

    public abstract void onShiftChange();

    protected void updateInfoOverlayOffset()
    {
        if (current_ray_target == null || !current_ray_target.name.Contains("manual_options") || HUD_setting > 1)
        {
            GetComponent<SecondaryScript>().updateInfoOverlayOffset(0.0f);
        }
        else
        {
            GetComponent<SecondaryScript>().updateInfoOverlayOffset(120.0f);
        }
    }

    //runs on Update() time
    protected IEnumerator seatCheck()
    {
        while (is_sitting == false)
        {
            yield return null;
            checkForSeats();
        }
        seat_check_coroutine = null;
    }

    protected abstract HUDInfo checkRayTarget();

    //called by seatCheck()
    protected abstract void checkForSeats();

    public abstract int getCurrPos();

    public abstract void assumePosition();

    public abstract void relinquishPosition();

    //called by checkForControlsAndInputs() on start of get up
    protected abstract void getUp();

    protected void updateIK()
    {
        //off by default
        my_animation_controller.setIKRightArm(false);
        my_animation_controller.setIKLeftArm(false);

        if (current_controllable != null) //IControllable, move hand
        {
            bool looking_right = (Vector3.SignedAngle(player_prefab.transform.forward, plr_camera.transform.forward, player_prefab.transform.up) > 0);
            my_animation_controller.setIKRightArm(looking_right);
            my_animation_controller.setIKLeftArm(!looking_right);

            IIKTargetable target_IK = current_controllable as IIKTargetable;
            if (target_IK != null)
            {
                //set hand agnostic stuff first
                my_animation_controller.setHandPose(target_IK.getHandPose());
                my_animation_controller.setLerpSpeed(target_IK.getLerpSpeed());

                //set the animation type
                my_animation_controller.setHandInteractionType(target_IK.getHandInteractionType());

                if (looking_right == true)
                {
                    //move the right arm target
                    my_animation_controller.setRightArmIKTransform(target_IK.getIKTarget(current_ray_target.gameObject));

                    //flip the arm rotation if the control needs it, usually for controls like the aux power lever
                    my_animation_controller.flipRightArmIKRotation(target_IK.getRightHandFlip());

                    //move the right hand to a specific spot offset from the actual target, usually when the the animation is press or pinch
                    my_animation_controller.adjustRightArmIKPosition(target_IK.getRightHandOffset());
                    my_animation_controller.setAnimatorLayerWeight("RightHandLayer", 1f);
                }
                else
                {
                    //move the left arm target
                    my_animation_controller.setLeftArmIKTransform(target_IK.getIKTarget(current_ray_target.gameObject));
                    my_animation_controller.setAnimatorLayerWeight("LeftHandLayer", 1f);
                }
            }
            //otherwise fallback to normal IK mode
            else
            {
                if (looking_right == true)
                {
                    my_animation_controller.setRightArmIKPosition(current_ray_target.transform.position);
                    my_animation_controller.setRightArmIKRotation(player_prefab.transform.localRotation);
                }
                else
                {
                    my_animation_controller.setLeftArmIKPosition(current_ray_target.transform.position);
                    my_animation_controller.setLeftArmIKRotation(player_prefab.transform.localRotation);
                }
            }
        }
    }

    //resets IK (called when not looking at a control or sensor)
    protected void resetIK()
    {
        my_animation_controller.setIKRightArm(false);
        my_animation_controller.setIKLeftArm(false);
        my_animation_controller.resetLerpSpeed();
        my_animation_controller.setAnimatorLayerWeight("RightHandLayer", 0.0f);
        my_animation_controller.setAnimatorLayerWeight("LeftHandLayer", 0.0f);
    }

    //runs on FixedUpdate() time (this code is meant to improve raycast consistency/avoid flickering)
    protected IEnumerator rayCheck()
    {
        float cooldown = 0.0f;
        current_ray_target = null;
        while (true)
        {
            if (plr_camera != null && is_active == true)
            {
                if (Physics.Raycast(new Ray(plr_camera.transform.position, plr_camera.transform.forward), out RaycastHit hit, RAYCAST_RANGE, LayerMask.GetMask("RayTarget")))
                {
                    if (current_ray_target == null || current_ray_target.name.CompareTo(hit.collider.gameObject.name) != 0)
                    {
                        current_ray_target = hit.collider.gameObject;
                        control_update_flag = true;
                    }
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
    protected IEnumerator controlCheck()
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
    protected void checkForControlsAndInputs()
    {
        if (plr_camera != null)
        {
            if (!paused && is_active)
            {
                //-----------------------------------------------CHECK FOR UNSEATING/SHIFTING--------------------------------------------------
                if (player_prefab.GetComponent<PlayerMove>().IsShifting() == false)
                {
                    //check if trying to unseat
                    if (UnityEngine.Input.GetKeyDown(input_options[13][0])) //trying to stand up
                    {
                        getUp();
                        return;
                    }

                    //check if trying to shift
                    if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift) || UnityEngine.Input.GetKeyDown(KeyCode.RightShift)) //trying to shift
                    {
                        player_prefab.GetComponent<PlayerMove>().SeatShift(getCurrPos());
                    }
                }

                //----------------------------------------------------CHECK FOR RAYTARGETS------------------------------------------------------
                if (current_ray_target != null) //check if raycast hit something
                {
                    //---------------------------------------------------HANDLE UI----------------------------------------------------------
                    HUDInfo temp_info = checkRayTarget();

                    //check if current HUDInfo is different from RayTarget HUDInfo
                    if (control_update_flag == true)
                    {
                        control_update_flag = false;
                        if (current_info != null && current_info.numOptions() > 0)
                        {
                            foreach (Button b in current_info.getButtons())
                            {
                                b.updateVisibility(false); //disconnect old buttons from visual updates
                            }
                        }
                        current_info = temp_info;
                        if (HUD_setting < 3) //default or minimized
                        {
                            initializePrimaryInfo();
                        }
                        updateCursorMode();
                        updateInfoOverlayOffset();
                        GetComponent<SecondaryScript>().updateSecondaryControlInformation(temp_info);
                    }
                    else
                    {
                        if (HUD_setting < 3) //default or minimized
                        {
                            updateButtons(temp_info);
                        }
                    }

                    //handle info showing/hiding
                    if (temp_info.hasInfo() == true)
                    {
                        //check if trying to show/hide info with tab key
                        if (UnityEngine.Input.GetKeyDown(KeyCode.Tab) && HUD_setting == 0)
                        {
                            GetComponent<SecondaryScript>().toggleControlInformationVisibility(temp_info);
                        }
                    }

                    //handle power consumption for power-consuming controls
                    if (temp_info.getConsumesPower() == true)
                    {
                        GetComponent<SecondaryScript>().updatePowerConsumption(temp_info);
                    }

                    //---------------------------------------------------HANDLE IK----------------------------------------------------------
                    updateIK();

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
                    primary_info.SetActive(true); //show UI indicator
                    GetComponent<SecondaryScript>().setSittingOverlayVisibility(HUD_setting == 0);
                    float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
                    if (current_controllable != null)
                    {
                        current_controllable.handleInputs(current_inputs, current_ray_target, dt, getCurrPos()); //call when all inputs have been checked
                    }
                    return;
                }
            }

            resetIK();
            GetComponent<SecondaryScript>().setSittingRightSideVisibility(false);
            primary_info.SetActive(false); //hide UI indicator if not looking at a control
            updateCursorMode(true);
            updateInfoOverlayOffset();
        }
    }
}