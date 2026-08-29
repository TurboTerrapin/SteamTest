/*
    BridgePrimaryScript.cs
    - Only runs after scene is loaded in as BridgeEnvironment
    - Handles sitting down/up AND control interactions
    - Manages the HUD display for control interaction
    - Sends user inputs to control script if looking at said control and within RAYCAST_RANGE
    - Handles transmitting IK targets for hand movement animations
    Contributor(s): Jake Schott, John Aylward
    Last Updated: 8/28/2026
*/

using TMPro;
using UnityEngine;

public class BridgePrimaryScript : PrimaryScript
{
    private int curr_pos = -1; //0 is Pilot, 1 is Tactician, 2 is Engineer, 3 is Captain

    public int currentSeat()
    {
        return curr_pos;
    }

    public override void onShiftChange()
    {
        GetComponent<SecondaryScript>().updateShiftIndicators(player_prefab.GetComponent<PlayerMove>().IsShifting(), curr_pos, ReferenceAssistor.Instance.seat_manager);
    }

    public override int getCurrPos()
    {
        return curr_pos;
    }

    //called by AnimatorHandler.cs when sit down animation is completed
    public override void assumePosition()
    {
        //if captain, trigger the seat enclosure animaiton
        if (curr_pos == 3)
        {
            ReferenceAssistor.Instance.seat_manager.encloseCaptainSeat();
        }

        player_prefab.GetComponent<CameraMove>().parentRotationLock = true;
        player_prefab.GetComponent<CameraMove>().SetCaptainMode(curr_pos == 3);
        player_prefab.GetComponent<CameraMove>().UnlockCamera(new Vector2(0.0f, 30.0f));

        my_animation_controller.setIKActive(true);
        my_animation_controller.setIKHead(true);

        onShiftChange();
        GetComponent<SecondaryScript>().setSittingOverlayVisibility(HUD_setting == 0);

        ray_target_check_coroutine = StartCoroutine(rayCheck());
        control_check_coroutine = StartCoroutine(controlCheck());
        player_prefab.GetComponent<PlayerMove>().SeatPush(curr_pos, true);
    }

    //called by AnimatorHandler.cs on end of get up
    public override void relinquishPosition()
    {
        player_prefab.GetComponent<CameraMove>().parentRotationLock = false;
        float[] rotations = new float[] { 0.0f, 0.0f, 135.0f, 0.0f };
        player_prefab.GetComponent<CameraMove>().UnlockCamera(new Vector2(rotations[curr_pos], 30.0f));
        my_animation_controller.setIKActive(true);
        my_animation_controller.setIKHead(true);
        my_animation_controller.setIKLeftArm(false);
        my_animation_controller.setIKRightArm(false);

        player_prefab.GetComponent<PlayerMove>().Initialize();

        ReferenceAssistor.Instance.seat_manager.getUp(curr_pos);

        curr_pos = -1;
        GetComponent<SecondaryScript>().onStationChange(curr_pos);
        seat_check_coroutine = StartCoroutine(seatCheck());
    }

    protected override void getUp()
    {
        is_sitting = false;

        //if captain, trigger the seat free animation
        if (curr_pos == 3)
        {
            ReferenceAssistor.Instance.seat_manager.releaseCaptainSeat();
        }

        my_animation_controller.setIKActive(false);

        current_ray_target = null;
        updateCursorMode();
        updateInfoOverlayOffset();

        primary_info.SetActive(false);
        GetComponent<SecondaryScript>().setSittingOverlayVisibility(false);
        GetComponent<SecondaryScript>().setSittingRightSideVisibility(false);

        resetButtons();
        onSittingChange();

        player_prefab.GetComponent<PlayerMove>().TriggerGetUpAnimation(curr_pos);
    }

    protected override void checkForSeats()
    {
        if (!paused && is_active && player_prefab != null && ReferenceAssistor.Instance.seat_manager != null)
        {
            int closest_seat = ReferenceAssistor.Instance.seat_manager.checkSeats(player_prefab.transform.position);
            if (closest_seat >= 0) //can sit
            {
                //update seat indicator color and information
                Color c = ReferenceAssistor.COLOR_OPTIONS[closest_seat];
                c.a = 0.84f;
                foreach (Transform t in default_view.transform.GetChild(0).GetChild(1))
                {
                    t.GetComponent<UnityEngine.UI.RawImage>().color = c;
                }
                c.a = 1.0f;
                default_view.transform.GetChild(0).GetChild(2).GetComponent<TMP_Text>().color = c;
                default_view.transform.GetChild(0).GetChild(2).GetComponent<TMP_Text>().SetText(ReferenceAssistor.STATION_NAMES[closest_seat] + " STATION");

                primary_info.SetActive(true);

                if (UnityEngine.Input.GetKeyDown(input_options[13][0])) //trying to sit down
                {
                    is_sitting = ReferenceAssistor.Instance.seat_manager.sitDown(closest_seat);
                    if (is_sitting == true)
                    {
                        curr_pos = closest_seat;
                        onSittingChange();
                        GetComponent<SecondaryScript>().onStationChange(curr_pos);
                        primary_info.SetActive(false);
                        player_prefab.GetComponent<CameraMove>().LockCamera();
                        player_prefab.GetComponent<CameraMove>().cameraHolder.parent = player_prefab.GetComponent<CameraMove>().headTransform;
                        player_prefab.GetComponent<PlayerMove>().TriggerSitDownAnimation(curr_pos);
                    }
                }
            }
            else //can't sit
            {
                primary_info.SetActive(false);
            }

            return;
        }
        primary_info.SetActive(false);
    }

    protected override HUDInfo checkRayTarget()
    {
        int script_holder = curr_pos; //0 pilot, 1 tactician, 2 engineer, 3 captain
        if (current_ray_target.transform.childCount > 1)
        {
            script_holder = 4; //4 general modules
        }
        current_controllable = ReferenceAssistor.Instance.module_handlers[script_holder].GetComponent(current_ray_target.transform.GetChild(0).name) as IControllable;
        current_describable = ReferenceAssistor.Instance.module_handlers[script_holder].GetComponent(current_ray_target.transform.GetChild(0).name) as IDescribable;

        HUDInfo temp_info = null;
        if (current_controllable != null) //IControllable
        {
            temp_info = current_controllable.getHUDinfo(current_ray_target.gameObject);
        }
        else //IDescribable
        {
            temp_info = current_describable.getHUDinfo(current_ray_target.gameObject);
        }

        return temp_info;
    }
}