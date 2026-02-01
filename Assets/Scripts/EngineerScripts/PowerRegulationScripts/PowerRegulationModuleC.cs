/*
    PowerRegulationModuleC.cs
    - Handles the timing mini-game in the engineer position
    Contributor(s): Jake Schott
    Last Updated: 10/23/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerRegulationModuleC : NetworkBehaviour, IControllable, IPowerRegulable
{
    //CLASS CONSTANTS
    private static float BUTTON_PUSH_TIME = 0.25f;
    private static float TIMING_BAR_MOVE_SPEED = 0.15f;
    private static float FURTHEST_TIMING_BAR_POINT = 0.1f;
    private static Vector3 BUTTON_PUSH_DIRECTION = new Vector3(0.002f, -0.004f, -0.002f);
    private static float[] STAGE_WIDTHS = new float[3] { 0.04f, 0.03f, 0.02f };
    private static float[] ARROW_LOCATIONS = new float[3] { -0.079f, 0.0f, 0.079f};

    private string CONTROL_NAME = "SEQUENCE COORDINATOR";
    private static string INFO_MESSAGE = "Time the synchronizer bar with the correct button three consecutive times to complete the module.";
    private List<string> CONTROL_DESCS = new List<string> { "SYNCHRONIZE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject prsc_display;
    public List<GameObject> prsc_buttons = null;

    private GameObject pointer_arrow;
    private GameObject timing_bar;

    private bool currently_active = false;

    private Vector3[] initial_positions = new Vector3[3];
    private int active_button = 0;
    private int stage = 0;
    private Coroutine timing_bar_coroutine = null;
    private Coroutine button_push_coroutine = null;

    private List<string> ray_targets = new List<string> { "prsc_button_a", "prsc_button_b", "prsc_button_c" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        pointer_arrow = prsc_display.transform.GetChild(0).gameObject;
        timing_bar = prsc_display.transform.GetChild(1).gameObject;

        for (int i = 0; i < 3; i++)
        {
            initial_positions[i] = prsc_buttons[i].transform.localPosition;
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        }

        hud_info = new HUDInfo(CONTROL_NAME);
        hud_info.setButtons(BUTTON_LISTS[0], 6);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setButtons(BUTTON_LISTS[index], 6);

        return hud_info;
    }

    private void checkTiming(int button_index)
    {
        float start_x = timing_bar.transform.localPosition.x - ((timing_bar.GetComponent<RectTransform>().sizeDelta.y * 0.5f) - 0.0025f);
        float end_x = timing_bar.transform.localPosition.x + ((timing_bar.GetComponent<RectTransform>().sizeDelta.y * 0.5f) + 0.0025f);

        float new_offset = Random.Range(-0.05f, 0.05f);
        int new_dir = Random.Range(0, 2);

        List<int> possible_locations = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            if (active_button != i)
            {
                possible_locations.Add(i);
            }
        }
        int new_arrow_position = possible_locations[Random.Range(0, 2)];

        if (button_index != active_button)
        {
            stageChangeRPC(0, new_arrow_position, new_offset, new_dir);
            return;
        }

        if (pointer_arrow.transform.localPosition.x > start_x && pointer_arrow.transform.localPosition.x < end_x)
        {
            if (stage == 2)
            {
                transmitModuleCompletionRPC();
            }
            else
            {
                stageChangeRPC(stage + 1, new_arrow_position, new_offset, new_dir);
            }
        }
        else
        {
            stageChangeRPC(0, new_arrow_position, new_offset, new_dir);
        }
    }

    IEnumerator timingBarBouncer(float starting_offset, int starting_dir)
    {
        timing_bar.transform.localPosition = new Vector3(starting_offset, 0.008f, 0.0f);

        int dir = 1; //1 is right, -1 is left
        if (starting_dir == 1)
        {
            dir = -1;
        }

        while (true)
        {
            float difference = Mathf.Min((FURTHEST_TIMING_BAR_POINT - (timing_bar.GetComponent<RectTransform>().sizeDelta.y * 0.5f) + 0.0025f), Time.deltaTime * TIMING_BAR_MOVE_SPEED);

            float new_x = timing_bar.transform.localPosition.x + (difference * dir);
            if (Mathf.Abs(new_x) > (FURTHEST_TIMING_BAR_POINT - (timing_bar.GetComponent<RectTransform>().sizeDelta.y * 0.5f) + 0.0025f))
            {
                difference = Mathf.Abs(new_x) - (FURTHEST_TIMING_BAR_POINT - (timing_bar.GetComponent<RectTransform>().sizeDelta.y * 0.5f) + 0.0025f);
                dir *= -1;
                new_x += (difference * dir);
            }

            timing_bar.transform.localPosition = new Vector3(new_x, 0.008f, 0.0f);

            yield return null;
        }
    }

    IEnumerator buttonPush(int index)
    {
        int curr_seat = ControlScript.Instance.currentSeat();

        for (int i = 0; i < 3; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(false);
            prsc_buttons[i].transform.localPosition = initial_positions[i];
        }

        Vector3 final_pos = initial_positions[index] + BUTTON_PUSH_DIRECTION;
        for (int i = 0; i <= 1; i++)
        {
            float half_time = BUTTON_PUSH_TIME * 0.5f;
            float push_time = half_time;

            while (push_time > 0.0f)
            {
                push_time = Mathf.Max(0.0f, push_time - Time.deltaTime);

                float push_percentage = 1.0f - (push_time / half_time);
                if (i == 1)
                {
                    push_percentage = (push_time / half_time);
                }

                prsc_buttons[index].transform.localPosition = Vector3.Lerp(initial_positions[index], final_pos, push_percentage);

                yield return null;
            }

            if (i == 0)
            {
                if (curr_seat == 2 && currently_active == true)
                {
                    checkTiming(index);
                }
            }
        }

        for (int i = 0; i < 3; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(currently_active);
        }

        button_push_coroutine = null;
    }

    public void resetToDefault()
    {
        if (currently_active == false)
        {
            return;
        }
        currently_active = false;
        prsc_display.SetActive(false);
        for (int i = 0; i < 3; i++)
        {
            BUTTON_LISTS[i][0].untoggle();
            BUTTON_LISTS[i][0].updateInteractable(false);
        }
    }

    public void unlockControl()
    {
        if (currently_active == true)
        {
            return;
        }
        currently_active = true;
        prsc_display.SetActive(true);
        timing_bar.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(0.005f, STAGE_WIDTHS[0]);
        if (NetworkManager.Singleton.IsHost == true)
        {
            stageChangeRPC(0, Random.Range(0, 3), Random.Range(-0.05f, 0.05f), Random.Range(0, 2));
        }
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (currently_active == false)
        {
            return;
        }

        int target_index = ray_targets.IndexOf(current_target.name);
        if (button_push_coroutine == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //press button
            {
                BUTTON_LISTS[target_index][0].toggle(0.1f);
                BUTTON_LISTS[target_index][0].updateInteractable(false);
                buttonPushRPC(target_index);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void buttonPushRPC(int index)
    {
        if (button_push_coroutine != null)
        {
            StopCoroutine(button_push_coroutine);
        }

        button_push_coroutine = StartCoroutine(buttonPush(index));
    }

    [Rpc(SendTo.Everyone)]
    private void stageChangeRPC(int new_stage, int arrow_location, float starting_offset, int starting_dir)
    {
        if (timing_bar_coroutine != null)
        {
            StopCoroutine(timing_bar_coroutine);
        }

        stage = new_stage;
        active_button = arrow_location;

        pointer_arrow.transform.localPosition = new Vector3(ARROW_LOCATIONS[arrow_location], -0.0035f, 0.0f);
        timing_bar.GetComponent<RectTransform>().sizeDelta = new Vector2(0.005f, STAGE_WIDTHS[new_stage]);
        timing_bar.transform.GetChild(0).transform.localPosition = new Vector3(0.0f, STAGE_WIDTHS[new_stage] * -0.5f, 0.0f);
        timing_bar.transform.GetChild(1).transform.localPosition = new Vector3(0.0f, STAGE_WIDTHS[new_stage] * 0.5f, 0.0f);

        timing_bar_coroutine = StartCoroutine(timingBarBouncer(starting_offset, starting_dir));

        if (button_push_coroutine == null)
        {
            for (int i = 0; i < 3; i++)
            {
                BUTTON_LISTS[i][0].updateInteractable(true);
            }
        }
    }

    //called by host when stage four reached (three successful timing events)
    [Rpc(SendTo.Everyone)]
    private void transmitModuleCompletionRPC()
    {
        GameObject.Find("PowerHandler").GetComponent<PowerRegulator>().moduleCompleted(this.GetType().Name);
    }
}