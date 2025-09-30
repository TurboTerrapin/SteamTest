/*
    ProbeVerticalMovement.cs
    - Turns lever
    - Adjusts screen
    - Affects probe
    Contributor(s): Jake Schott
    Last Updated: 8/22/2025
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ProbeVerticalMovement : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float LEVER_SPEED = 100.0f;
    private static float PROBE_SPEED = 0.1f;

    private string CONTROL_NAME = "PROBE VERTICAL MOVEMENT";
    private List<string> CONTROL_DESCS = new List<string> {"DESCEND", "ASCEND"};
    private List<int> CONTROL_INDEXES = new List<int>() {2,0};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject vertical_lever;
    public GameObject vertical_display;
    public GameObject vertical_probe_icon_display;

    private bool is_powered = false;
    private GameObject probe;
    private float vertical_lever_angle = 0.0f;
    private Vector3 probe_position = new Vector3(0.0f, 0.0f, 0.0f);
    private Coroutine vertical_adjustment_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        hud_info.setButtons(BUTTONS, 7);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public void updateAltimeterScreen()
    {
        GameObject altimeter = vertical_display.transform.GetChild(0).GetChild(2).gameObject;

        //get current altitude
        float current_altitude = probe.transform.position.y;

        //get number markers
        int smallest_number = (((int)(current_altitude)) / 10) * 10;
        int next_number = smallest_number + 10;
        if (current_altitude < 0.0f)
        {
            next_number = smallest_number - 10;
        }

        //define order of markers
        List<GameObject> bars = new List<GameObject>();
        int[] marker_indices = new int[4];
        int[] corresponding_markers = new int[4];
        int marker_index = 18 - (int)((current_altitude % 5.0f) / 1.0f); //defines top marker

        for (int i = 0; i < 4; i++) //define other markers (every 5th marker)
        {
            marker_indices[i] = marker_index - (i * 5);
        }
        if ((Mathf.Abs(current_altitude) % 10.0f < 5.0f)) //swap between number/midpoint halfway
        {
            corresponding_markers[0] = 0;
            corresponding_markers[1] = 1;
            corresponding_markers[2] = 2;
            corresponding_markers[3] = 3;
        }
        else
        {
            corresponding_markers[0] = 1;
            corresponding_markers[1] = 0;
            corresponding_markers[2] = 3;
            corresponding_markers[3] = 2;
            //if negative, switch numbers
            if (current_altitude < 0.0f)
            {
                int temp = smallest_number;
                smallest_number = next_number;
                next_number = temp;
            }
        }

        //set text for text markers
        altimeter.transform.GetChild(0).transform.GetChild(0).GetComponent<TMP_Text>().SetText(next_number.ToString() + "m");
        altimeter.transform.GetChild(2).transform.GetChild(0).GetComponent<TMP_Text>().SetText(smallest_number.ToString() + "m");

        //define order of markers
        for (int i = 0; i < 17; i++)
            {
                bool marked = false;
                for (int x = 0; x < 4; x++)
                {
                    if (i == marker_indices[x])
                    {
                        bars.Add(altimeter.transform.GetChild(corresponding_markers[x]).gameObject);
                        marked = true;
                        break;
                    }
                }
                if (marked == false)
                {
                    bars.Add(altimeter.transform.GetChild(i + 4).gameObject);
                }
            }
        //hide all markers to start
        for (int i = 0; i < 21; i++)
        {
            altimeter.transform.GetChild(i).gameObject.SetActive(false);
        }
        //set positions and active state of each marker
        float shift = ((-current_altitude % 1.0f) / 1.0f) * 0.01f; //0.01 in distance between markers equals 1 meter
        for (int i = 0; i < 17; i++)
        {
            bars[i].SetActive(true);
            bars[i].transform.localPosition = new Vector3(bars[i].transform.localPosition.x, (0.01f * i) - 0.08f + shift, 0.0f);
        }
    }

    private void displayAdjustment()
    {
        //update lever position
        vertical_lever.transform.localRotation = Quaternion.Euler(-70f - vertical_lever_angle, 180f, -90f);

        //update probe
        if (probe != null)
        {
            probe.transform.localPosition = probe_position;
            probe.GetComponent<Probe>().updateDistance();

            //lastly, update altitude screen
            updateAltimeterScreen();
        }
    }

    public void linkProbe(GameObject new_probe)
    {
        probe = new_probe;
        for (int i = 0; i <= 1; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }
        updateAltimeterScreen();
        vertical_display.transform.GetChild(0).gameObject.SetActive(is_powered);
        vertical_probe_icon_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
    }

    public void unlinkProbe()
    {
        probe = null;
        for (int i = 0; i <= 1; i++)
        {
            BUTTONS[i].updateInteractable(false);
        }
        vertical_display.transform.GetChild(0).gameObject.SetActive(false);
        vertical_probe_icon_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
    }

    private bool isNeutralState()
    {
        return (vertical_lever_angle == 0.0f);
    }

    IEnumerator verticalAdjustment()
    {
        while (keys_down.Count > 0 || !isNeutralState())
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            if (probe != null)
            {
                probe_position = probe.transform.localPosition;
            }

            int vertical_direction = 0;

            if (is_powered == true)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], keys_down) && probe != null)
                {
                    vertical_direction += 1;
                }
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && probe != null)
                {
                    vertical_direction -= 1;
                }
            }

            if (vertical_direction != 0)
            {
                if (vertical_direction > 0)
                {
                    vertical_lever_angle = Mathf.Min(35.0f, vertical_lever_angle + dt * LEVER_SPEED);
                }
                else
                {
                    vertical_lever_angle = Mathf.Max(-35.0f, vertical_lever_angle - dt * LEVER_SPEED);
                }
            }
            else
            {
                if (vertical_lever_angle > 0.0f)
                {
                    vertical_lever_angle = Mathf.Max(0.0f, vertical_lever_angle - dt * LEVER_SPEED);
                }
                else
                {
                    vertical_lever_angle = Mathf.Min(0.0f, vertical_lever_angle + dt * LEVER_SPEED);
                }
            }

            if (Mathf.Abs(vertical_lever_angle) > 0.0f)
            {
                probe_position += probe.transform.up * vertical_lever_angle * dt * PROBE_SPEED;
            }

            if (vertical_lever_angle != 0.0f)
            {
                transmitProbeVerticalAdjustmentRPC(probe_position, vertical_lever_angle);
            }

            keys_down.Clear();
            yield return null;
        }

        vertical_adjustment_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        vertical_display.SetActive(true);
        vertical_probe_icon_display.SetActive(true);
        BUTTONS[0].updateInteractable(probe != null);
        BUTTONS[1].updateInteractable(probe != null);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        vertical_display.SetActive(false);
        vertical_probe_icon_display.SetActive(false);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        keys_down = inputs;
        if (vertical_adjustment_coroutine == null)
        {
            for (int i = 0; i < CONTROL_INDEXES.Count; i++)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    vertical_adjustment_coroutine = StartCoroutine(verticalAdjustment());
                    return;
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitProbeVerticalAdjustmentRPC(Vector3 new_pos, float ang)
    {
        vertical_lever_angle = ang;
        probe_position = new_pos;
        displayAdjustment();
    }
}