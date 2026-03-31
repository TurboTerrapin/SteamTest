/*
    ProximityMapOptions.cs
    - Handles inputs for map zoom, map configuration
    - Zooms the lines for the map, tells ProximityMap to zoom the objects accordingly
    Contributor(s): Jake Schott
    Last Updated: 3/7/2026
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class ProximityMapOptions : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float ZOOM_SPEED = 1.0f;
    private Vector3 SLIDER_FINAL_POS = new Vector3(0.0f, -0.0394f, -0.0934f);

    private string CONTROL_NAME = "PROXIMITY MAP";
    private List<string> CONTROL_DESCS = new List<string> { "ZOOM OUT", "ZOOM IN" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject slider;
    public GameObject zoom_display;
    public GameObject map_display;

    private ProximityMap proximity_map;

    private bool is_powered = false;
    private float zoom = 1.0f;

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public int finger_position = 0;

    private void Start()
    {
        proximity_map = GetComponent<ProximityMap>();

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        hud_info.setButtons(BUTTONS);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
    public Transform getIKTarget(GameObject current_target)
    {
        //int index = ray_targets.IndexOf(current_target.name);
        return IK_targets[0].transform;
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
    public float getZoom()
    {
        return zoom;
    }

    private void displayZoomAdjustment()
    {
        //zoom items
        proximity_map.zoomMap();

        //update zoom slider position
        slider.transform.localPosition = Vector3.Lerp(Vector3.zero, SLIDER_FINAL_POS, 1.0f - zoom);

        //update zoom display
        zoom_display.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = zoom;
        zoom_display.transform.GetChild(2).localPosition = new Vector3(0.0f, Mathf.Lerp(-0.045f, 0.045f, zoom), 0.0f);
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        //check zoom inputs
        int zoom_direction = 0;
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to increment
        {
            zoom_direction += 1;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))  //Q to decrement
        {
            zoom_direction -= 1;
        }
        if (zoom_direction != 0)
        {
            if (zoom_direction > 0)
            {
                zoom = Mathf.Min(1.0f, zoom + dt * ZOOM_SPEED);
            }
            else
            {
                zoom = Mathf.Max(0.0f, zoom - dt * ZOOM_SPEED);
            }
            BUTTONS[0].updateInteractable(zoom > 0.0f);
            BUTTONS[1].updateInteractable(zoom < 1.0f);
            transmitMapZoomAdjustmentRPC(zoom);
        }
    }

    public void resetToDefault()
    {
        zoom = 1.0f;
        displayZoomAdjustment();
    }

    public void powerOn(int position)
    {
        is_powered = true;
        zoom_display.SetActive(true);
        BUTTONS[0].updateInteractable(zoom > 0.0f);
        BUTTONS[1].updateInteractable(zoom < 1.0f);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        zoom_display.SetActive(false);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitMapZoomAdjustmentRPC(float zm)
    {
        zoom = zm;
        displayZoomAdjustment();
    }
}