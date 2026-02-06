/*
    ComputerRegulator.cs
    - Pushes in the four movement buttons
    - Controls cursor
    - Handles generating "algorithmic patterns"
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ComputerRegulator : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float BUTTON_SPEED = 15.0f;
    private static Vector3 BUTTON_MOVE_DIRECTION = new Vector3(0.002f, -0.004f, -0.002f);
    private static float CONFIRM_BUTTON_TIME = 0.25f;
    private static float CURSOR_MOVE_SPEED = 0.1f;
    private static Vector2 CURSOR_BOUNDS = new Vector2(0.069f, 0.095f);
    private static Color[] COLOR_OPTIONS = new Color[3] { new Color(0.129f, 1f, 0.04f, 0.2f), new Color(0.69f, 0f, 0.69f, 0.2f), new Color(0.84f, 0.62f, 0f, 0.2f) };

    private string CONTROL_NAME = "COMPUTER REGULATOR";
    private static string INFO_MESSAGE = "Controls overall ship computer infrastructure to handle malfunctions or hack attempts.";
    private List<string> CONTROL_DESCS = new List<string> { "UP", "DOWN", "SELECT", "LEFT", "RIGHT" };
    private List<int> CONTROL_INDEXES = new List<int>() { 0, 2, 6, 1, 3 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject computer_regulator_display;

    private GameObject cursor;
    private GameObject shapes_holder;

    public List<GameObject> computer_regulator_cursor_buttons = null;
    public GameObject computer_regulator_confirm_button;

    private bool is_powered = false;
    private GameObject[] shapes = new GameObject[10] { null, null, null, null, null, null, null, null, null, null };
    private int[] shape_colors = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private Vector3[] initial_positions = new Vector3[5];
    private Vector3[] final_positions = new Vector3[5];
    private float[] cursor_movement_factors = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f }; //up, down, left, right
    private Vector2 cursor_position = new Vector2(0.0f, 0.0f);
    private Coroutine cursor_adjustment_coroutine = null;
    private Coroutine cursor_confirm_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;

    //used to space out the different shapes
    private struct area
    {
        public Vector2 tlc;
        public Vector2 brc;

        public area(Vector2 top_left_coordinate, Vector2 bottom_right_coordinate)
        {
            tlc = top_left_coordinate;
            brc = bottom_right_coordinate;
        }

        public bool pointIsInArea(Vector2 to_test)
        {
            return ((to_test.x > tlc.x && to_test.x < brc.x) && (to_test.y < tlc.y && to_test.y > brc.y));
        }

        public bool canFitShape(float shape_size)
        {
            shape_size += 0.015f; //add a little cushion
            float area_width = brc.x - tlc.x;
            float area_height = tlc.y - brc.y;
            return (area_width > shape_size && area_height > shape_size);
        }

        public bool isOverlapping(area other)
        {
            return (pointIsInArea(other.tlc) || pointIsInArea(other.brc) || pointIsInArea(new Vector2(other.tlc.x, other.brc.y)) || pointIsInArea(new Vector2(other.brc.x, other.tlc.y)));
        }

        public List<Vector2> addPoints(List<Vector2> points_list)
        {
            points_list.Add(brc);
            points_list.Add(tlc);
            points_list.Add(new Vector2(brc.x, tlc.y));
            points_list.Add(new Vector2(tlc.x, brc.y));
            return points_list;
        }
    }

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);

        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[3], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[4], CONTROL_INDEXES[4], false, false));

        hud_info.setButtons(BUTTONS, 9);
        hud_info.setInfo(INFO_MESSAGE);

        cursor = computer_regulator_display.transform.GetChild(1).gameObject;
        shapes_holder = computer_regulator_display.transform.GetChild(0).gameObject;

        for (int i = 0; i < 5; i++)
        {
            if (i != 2)
            {
                int index = i;
                if (i > 2)
                {
                    index -= 1;
                }
                initial_positions[i] = computer_regulator_cursor_buttons[index].transform.localPosition;
            }
            else
            {
                initial_positions[i] = computer_regulator_confirm_button.transform.localPosition;
            }
            final_positions[i] = initial_positions[i] + BUTTON_MOVE_DIRECTION;
        }
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    //returns -1 if none
    private int getSelectedShape()
    {
        int selected_shape = -1;
        float closest_dist = 9999.9f;
        for (int i = 0; i < 10; i++)
        {
            if (shapes[i] != null)
            {
                float dist = Vector2.Distance(cursor_position, new Vector2(shapes[i].transform.localPosition.x, shapes[i].transform.localPosition.y));
                if (dist < closest_dist && (dist < shapes[i].GetComponent<RectTransform>().sizeDelta.x * 0.5f + 0.003f))
                {
                    selected_shape = i;
                    closest_dist = dist;
                }
            }
        }
        return selected_shape;
    }

    private void displayAdjustment()
    {
        //push buttons
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            if (i >= 2)
            {
                index += 1;
            }
            computer_regulator_cursor_buttons[i].transform.localPosition = Vector3.Lerp(initial_positions[index], final_positions[index], cursor_movement_factors[i]);
        }

        //place cursor
        cursor.transform.localPosition = new Vector3(cursor_position.x, cursor_position.y, 0.0f);

        //highlight current
        int highlighted_shape = getSelectedShape();
        for (int i = 0; i < 10; i++)
        {
            if (shapes[i] != null)
            {
                shapes[i].transform.GetChild(0).gameObject.SetActive(i != highlighted_shape);
                if (i == highlighted_shape)
                {
                    shapes[i].GetComponent<UnityEngine.UI.RawImage>().color = COLOR_OPTIONS[shape_colors[i]];
                }
                else
                {
                    shapes[i].GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 1.0f, 1.0f, 0.2f);
                }
            }
        }
    }

    private bool isNeutralState()
    {
        for (int i = 0; i <  4; i++)
        {
            if (cursor_movement_factors[i] != 0.0f)
            {
                return false;
            }
        }
        return true;
    }

    IEnumerator cursorAdjustment()
    {
        while (keys_down.Count > 0 || !isNeutralState())
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            cursor_position = new Vector2(cursor.transform.localPosition.x, cursor.transform.localPosition.y);

            if (is_powered == true)
            {
                //check inputs/return buttons to default
                for (int i = 0; i < 4; i++)
                {
                    int index = i;
                    if (i >= 2)
                    {
                        index += 1;
                    }
                    if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[index], keys_down))
                    {
                        cursor_movement_factors[i] = Mathf.Min(1.0f, cursor_movement_factors[i] + dt * BUTTON_SPEED);
                    }
                    else
                    {
                        cursor_movement_factors[i] = Mathf.Max(0.0f, cursor_movement_factors[i] - dt * BUTTON_SPEED);
                    }
                }
            }

            //update cursor position
            if (Mathf.Abs(cursor_movement_factors[0] - cursor_movement_factors[1]) > 0.0f)
            {
                cursor_position.y += (cursor_movement_factors[0] - cursor_movement_factors[1]) * dt * CURSOR_MOVE_SPEED;
                cursor_position.y = Mathf.Clamp(cursor_position.y, CURSOR_BOUNDS.y * -1.0f + 0.01f, CURSOR_BOUNDS.y - 0.01f);
            }
            if (Mathf.Abs(cursor_movement_factors[3] - cursor_movement_factors[2]) > 0.0f)
            {
                cursor_position.x += (cursor_movement_factors[3] - cursor_movement_factors[2]) * dt * CURSOR_MOVE_SPEED;
                cursor_position.x = Mathf.Clamp(cursor_position.x, CURSOR_BOUNDS.x * -1.0f + 0.01f, CURSOR_BOUNDS.x - 0.01f);
            }

            for (int i = 0; i < 4; i++)
            {
                if (cursor_movement_factors[i] != 1.0f)
                {
                    transmitCursorAdjustmentRPC(cursor_position, cursor_movement_factors[0], cursor_movement_factors[1], cursor_movement_factors[2], cursor_movement_factors[3]);
                    break;
                }
            }

            keys_down.Clear();
            yield return null;
        }

        cursor_adjustment_coroutine = null;
    }

    IEnumerator cursorConfirmation()
    {
        for (int i = 0; i <= 1; i++)
        {
            float half_time = CONFIRM_BUTTON_TIME * 0.5f;
            float push_time = half_time;

            while (push_time > 0.0f)
            {
                push_time = Mathf.Max(0.0f, push_time - Time.deltaTime);

                float push_percentage = 1.0f - (push_time / half_time);
                if (i == 1)
                {
                    push_percentage = (push_time / half_time);
                }

                computer_regulator_confirm_button.transform.localPosition = Vector3.Lerp(initial_positions[2], final_positions[2], push_percentage);

                yield return null;
            }

            if (i == 0)
            {
                if (NetworkManager.Singleton.IsHost == true)
                {
                    int selected_shape = getSelectedShape();
                    if (selected_shape >= 0)
                    {
                        transmitShapeRemovalRPC(selected_shape);
                    }
                }
            }
        }

        BUTTONS[2].untoggle();
        BUTTONS[2].updateInteractable(true);

        cursor_confirm_coroutine = null;
    }
    
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        keys_down = inputs;
        if (cursor_confirm_coroutine == null)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[2], inputs))
            {
                BUTTONS[2].toggle();
                BUTTONS[2].updateInteractable(false);
                transmitCursorConfirmRPC();
            }
        }

        if (cursor_adjustment_coroutine == null)
        {
            for (int i = 0; i < CONTROL_INDEXES.Count; i++)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[i], inputs) && i != 2)
                {
                    cursor_adjustment_coroutine = StartCoroutine(cursorAdjustment());
                    return;
                }
            }
        }
    }

    private Vector2 getRandomLocation(float size, int c)
    {
        Vector2 valid_location = Vector2.zero;
        List<area> unoccupied_areas = new List<area>();
        List<area> occupied_areas = new List<area>();

        List<Vector2> connecting_points = new List<Vector2>();

        area default_space = new area(new Vector2(CURSOR_BOUNDS.x * -1.0f, CURSOR_BOUNDS.y), new Vector2(CURSOR_BOUNDS.x, CURSOR_BOUNDS.y * -1.0f));
        connecting_points = default_space.addPoints(connecting_points);

        //check existing shapes
        for (int i = 0; i < 10; i++)
        {
            if (shapes[i] != null)
            {
                area occupied_space = new area();
                occupied_space.tlc = new Vector2(shapes[i].transform.localPosition.x - (shapes[i].GetComponent<RectTransform>().sizeDelta.x * 0.5f), shapes[i].transform.localPosition.y + (shapes[i].GetComponent<RectTransform>().sizeDelta.y * 0.5f));
                occupied_space.brc = new Vector2(shapes[i].transform.localPosition.x + (shapes[i].GetComponent<RectTransform>().sizeDelta.x * 0.5f), shapes[i].transform.localPosition.y - (shapes[i].GetComponent<RectTransform>().sizeDelta.y * 0.5f));
                
                connecting_points = occupied_space.addPoints(connecting_points);
                occupied_areas.Add(occupied_space);
            }
        }

        //start at 1 to skip bottom right corner
        for (int i = 1; i < connecting_points.Count; i++)
        {
            for (int k = 0; k < connecting_points.Count; k++)
            {
                if (connecting_points[i] != connecting_points[k])
                {
                    area rectangular_space = new area(connecting_points[i], connecting_points[k]);

                    //ensure tlc is actually top-left coordinate
                    if (rectangular_space.tlc.x > rectangular_space.brc.x)
                    {
                        float switch_x = rectangular_space.brc.x;
                        rectangular_space.brc.x = rectangular_space.tlc.x;
                        rectangular_space.tlc.x = switch_x;
                    }

                    //ensure brc is actually bottom-right coordinate
                    if (rectangular_space.brc.y > rectangular_space.tlc.y)
                    {
                        float switch_y = rectangular_space.brc.y;
                        rectangular_space.brc.y = rectangular_space.tlc.y;
                        rectangular_space.tlc.y = switch_y;
                    }

                    unoccupied_areas.Add(rectangular_space);
                }
            }
        }

        List<area> candidate_areas = new List<area>();

        //filter out spaces
        foreach (area possible_area in unoccupied_areas)
        {
            bool successful_candidate = true;

            //make sure to not include already included areas
            for (int i = 0; i < candidate_areas.Count; i++)
            {
                if (possible_area.tlc == candidate_areas[i].tlc && possible_area.brc == candidate_areas[i].brc)
                {
                    successful_candidate = false;
                }
            }

            //not big enough
            if (possible_area.canFitShape(size) == false)
            {
                successful_candidate = false;
            }
            
            //compare against occupied areas
            for (int i = 0; i < occupied_areas.Count; i++)
            {
                //check if occupied or overlapping with occupied
                if ((possible_area.tlc == occupied_areas[i].tlc && possible_area.brc == occupied_areas[i].brc) || (possible_area.isOverlapping(occupied_areas[i]) == true)) 
                {
                    successful_candidate = false;
                    break;
                }
            }

            if (successful_candidate == true) //passes tests, add as candidate
            {
                candidate_areas.Add(possible_area);
            }
        }

        //pick candidate area with greatest size to spread out as much as possible
        if (candidate_areas.Count > 0)
        {
            int candidate_to_choose = 0;
            float greatest_dist = 0.0f;
            for (int i = 0; i < candidate_areas.Count; i++)
            {
                float dist = Vector2.Distance(candidate_areas[i].tlc, candidate_areas[i].brc);
                if (dist > greatest_dist)
                {
                    candidate_to_choose = i;
                    greatest_dist = dist;
                }
            }
            valid_location.x = Random.Range(candidate_areas[candidate_to_choose].tlc.x + (size * 0.5f) + 0.0075f, candidate_areas[candidate_to_choose].brc.x - (size * 0.5f) - 0.0075f);
            valid_location.y = Random.Range(candidate_areas[candidate_to_choose].brc.y + (size * 0.5f) + 0.0075f, candidate_areas[candidate_to_choose].tlc.y - (size * 0.5f) - 0.0075f);
        }

        return valid_location;
    }

    private void generateNewShape(int slot, int shape_index, float shape_size, Vector2 location, int color_index)
    {
        //base off template
        GameObject new_shape = GameObject.Instantiate(shapes_holder.transform.GetChild(0).GetChild(shape_index).gameObject, shapes_holder.transform);

        //resize
        float cover_up_difference = new_shape.GetComponent<RectTransform>().sizeDelta.x - new_shape.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.x;
        new_shape.GetComponent<RectTransform>().sizeDelta = new Vector2(shape_size, shape_size);
        new_shape.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(shape_size - cover_up_difference, shape_size - cover_up_difference);

        //record color
        shape_colors[slot] = color_index;

        //place
        new_shape.transform.localPosition = new Vector3(location.x, location.y, 0.0f);

        //make visible
        new_shape.SetActive(true);

        //set slot
        shapes[slot] = new_shape;
    }

    private void clearAllShapes()
    {
        for (int i = shapes_holder.transform.childCount - 1; i > 0; i--)
        {
            GameObject.Destroy(shapes_holder.transform.GetChild(i).gameObject);
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;

        computer_regulator_display.SetActive(true);

        cursor_position = new Vector2(0.0f, 0.0f);
        displayAdjustment();

        for (int i = 0; i < 5; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }

        if (NetworkManager.Singleton.IsHost == true)
        {
            for (int i = 0; i < 10; i++)
            {
                float shape_size = Random.Range(0.015f, 0.02f);
                int shape_index = Random.Range(0, 3);
                int color_index = Random.Range(0, 3);
                Vector2 shape_location = getRandomLocation(shape_size, i);
                transmitShapeAdditionRPC(i, shape_index, shape_size, shape_location, color_index);
            }
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

        clearAllShapes();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitCursorAdjustmentRPC(Vector2 new_pos, float up, float down, float left, float right)
    {
        cursor_movement_factors[0] = up;
        cursor_movement_factors[1] = down;
        cursor_movement_factors[2] = left;
        cursor_movement_factors[3] = right;
        cursor_position = new_pos;
        displayAdjustment();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitCursorConfirmRPC()
    {
        if (cursor_confirm_coroutine != null)
        {
            StopCoroutine(cursor_confirm_coroutine);
        }

        cursor_confirm_coroutine = StartCoroutine(cursorConfirmation());
    }

    [Rpc(SendTo.Everyone)]
    private void transmitShapeAdditionRPC(int slot, int si, float size, Vector2 loc, int ci)
    {
        if (shapes[slot] != null)
        {
            GameObject.Destroy(shapes[slot].gameObject);
        }
        generateNewShape(slot, si, size, loc, ci);
        displayAdjustment();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitShapeRemovalRPC(int slot)
    {
        if (shapes[slot] != null)
        {
            GameObject.Destroy(shapes[slot].gameObject);
            shapes[slot] = null;
        }
        displayAdjustment();
    }
}