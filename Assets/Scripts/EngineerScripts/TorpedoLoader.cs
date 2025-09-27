/*
    TorpedoLoader.cs
    - Handles the loading of torpedoes 
    Contributor(s): Jake Schott
    Last Updated: 9/22/2025
*/

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TorpedoLoader : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SELECTION_ADJUSTMENT_TIME = 0.25f;
    private static float DIRECTION_ADJUSTMENT_TIME = 0.5f;
    private static float LOAD_CONFIRMATION_TIME = 2.0f;

    private string[] CONTROL_NAMES = new string[] { "TORPEDO TYPE SELECTOR", "TORPEDO BAY SELECTOR", "TORPEDO BAY LOADER" };
    private List<string> CONTROL_DESCS = new List<string> { "SELECT LEFT", "SELECT RIGHT", "SHIFT LEFT", "SHIFT RIGHT", "LOAD" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5, 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public Material lit_green;
    public Material lit_red;
    public Material unlit_neon;

    public GameObject torpedo_selection_display;
    public GameObject torpedo_direction_display;
    public GameObject ship_overview_torpedo_information;

    public GameObject torpedo_selection_switch;
    public GameObject torpedo_direction_slider;
    public GameObject torpedo_confirmation_switch;

    private EngineerInventory engineer_inventory;

    private bool is_powered = false;
    private int[] torpedo_bay_slots = new int[4] { -1, -1, -1, -1 };
    private int current_torpedo_selection = 0;
    private int current_torpedo_bay = 0;
    private Vector3 torpedo_direction_slider_initial_position;
    private Vector3 torpedo_bay_adjustment_direction = new Vector3(-0.059f, 0.0f, -0.059f);
    private Coroutine torpedo_direction_adjustment_coroutine = null;
    private Coroutine torpedo_selection_adjustment_coroutine = null;
    private Coroutine torpedo_confirmation_coroutine = null;

    private List<string> ray_targets = new List<string> { "torpedo_loader_selection_switch", "torpedo_loader_direction_slider", "torpedo_loader_confirm_switch" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        engineer_inventory = GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerInventory>();

        torpedo_direction_slider_initial_position = torpedo_direction_slider.transform.localPosition;

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));

        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[1], false, true));

        BUTTON_LISTS[2].Add(new Button(CONTROL_DESCS[4], CONTROL_INDEXES[2], false, true));

        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        hud_info.setButtons(BUTTON_LISTS[0], 7);

        displayTorpedoSelectionAdjustment();
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);

        hud_info.setTitle(CONTROL_NAMES[index]);

        if (index < 2)
        {
            hud_info.setButtons(BUTTON_LISTS[index], 7);
        }
        else
        {
            hud_info.setButtons(BUTTON_LISTS[index], 6);
        }

        return hud_info;
    }

    private void displayTorpedoSelectionAdjustment()
    {
        UnityEngine.UI.RawImage torpedo_icon = torpedo_selection_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>();
        TMP_Text torpedo_text = torpedo_selection_display.transform.GetChild(1).GetComponent<TMP_Text>();

        Color torpedo_color = engineer_inventory.getItemColor(1, current_torpedo_selection);

        //make transparent if none available
        float a = 1.0f;
        if (engineer_inventory.getItemQuantity(1, current_torpedo_selection) <= 0)
        {
            a = 0.2f;
        }
        torpedo_color = new Color(torpedo_color.r, torpedo_color.g, torpedo_color.b, a);

        //set icon
        torpedo_icon.color = torpedo_color;
        torpedo_icon.texture = engineer_inventory.getItemTexture(1, current_torpedo_selection);

        //set text
        torpedo_text.color = torpedo_color;
        torpedo_text.SetText((engineer_inventory.getItemName(1, current_torpedo_selection) + " TORPEDO").ToUpper());

        //adjust lit indicator on confirmation switch
        if (is_powered == true)
        {
            changeDialLitIndicator((engineer_inventory.getItemQuantity(1, current_torpedo_selection) > 0) && (torpedo_bay_slots[current_torpedo_bay] < 0));
        }
    }

    private void darkenTorpedoDirections()
    {
        //darken all sections
        foreach (Transform child in torpedo_direction_display.transform)
        {
            child.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
            Color occupied_torpedo_color = child.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color;
            child.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(occupied_torpedo_color.r, occupied_torpedo_color.g, occupied_torpedo_color.b, 0.2f);
        }
    }

    private void displayTorpedoDirectionAdjustment()
    {
        //highlight current section
        torpedo_direction_display.transform.GetChild(current_torpedo_bay).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        Color occupied_torpedo_color = torpedo_direction_display.transform.GetChild(current_torpedo_bay).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color;
        torpedo_direction_display.transform.GetChild(current_torpedo_bay).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(occupied_torpedo_color.r, occupied_torpedo_color.g, occupied_torpedo_color.b, 1.0f);

        //adjust lit indicator on confirmation switch
        if (is_powered == true)
        {
            changeDialLitIndicator((engineer_inventory.getItemQuantity(1, current_torpedo_selection) > 0) && (torpedo_bay_slots[current_torpedo_bay] < 0));
        }
    }

    private void changeDialLitIndicator(bool is_green)
    {
        if (is_green)
        {
            torpedo_confirmation_switch.transform.GetChild(0).GetComponent<Renderer>().material = lit_green;
        }
        else
        {
            torpedo_confirmation_switch.transform.GetChild(0).GetComponent<Renderer>().material = lit_red;
        }
    }

    //returns true if current torpedo bay is unoccupied, there's at least one torpedo of that type in the inventory, and no coroutines running
    private bool getCurrentlyLoadable()
    {
        if (is_powered == false)
        {
            return false;
        }

        if (engineer_inventory.getItemQuantity(1, current_torpedo_selection) <= 0)
        {
            return false;
        }

        if (torpedo_bay_slots[current_torpedo_bay] >= 0)
        {
            return false;
        }

        if (torpedo_selection_adjustment_coroutine != null || torpedo_direction_adjustment_coroutine != null || torpedo_confirmation_coroutine != null)
        {
            return false;
        }

        return true;
    }

    //handles the left-right switch that switches between the torpedo types/colors
    IEnumerator torpedoSelectionAdjustment(bool left)
    {
        float destination_rotation = 25.0f;
        if (left == false)
        {
            destination_rotation = -25.0f;
        }

        float anim_time = SELECTION_ADJUSTMENT_TIME;
        for (int i = 0; i <= 1; i++)
        {
            float half_time = SELECTION_ADJUSTMENT_TIME * 0.5f;
            float curr_time = half_time;

            while (curr_time > 0.0f)
            {
                curr_time = Mathf.Max(0.0f, curr_time - Time.deltaTime);

                float switch_percentage = 1.0f - (curr_time / half_time);
                if (i == 1)
                {
                    switch_percentage = (curr_time / half_time);
                }

                torpedo_selection_switch.transform.localRotation = Quaternion.Euler(0.0f, Mathf.Lerp(0.0f, destination_rotation, switch_percentage), 90.0f);

                yield return null;
            }

            if (i == 0)
            {
                displayTorpedoSelectionAdjustment();
            }
        }

        BUTTON_LISTS[0][0].untoggle();
        BUTTON_LISTS[0][1].untoggle();
        BUTTON_LISTS[0][0].updateInteractable(is_powered);
        BUTTON_LISTS[0][1].updateInteractable(is_powered);

        torpedo_selection_adjustment_coroutine = null;

        BUTTON_LISTS[2][0].updateInteractable(getCurrentlyLoadable());
    }

    //handles the slider that selects which bay to load the torpedo in
    IEnumerator torpedoDirectionAdjustment()
    {
        Vector3 start_pos = torpedo_direction_slider.transform.localPosition;
        Vector3 dest_pos = Vector3.Lerp(torpedo_direction_slider_initial_position, torpedo_direction_slider_initial_position + torpedo_bay_adjustment_direction, current_torpedo_bay / 3.0f);

        darkenTorpedoDirections();

        float anim_time = DIRECTION_ADJUSTMENT_TIME;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            torpedo_direction_slider.transform.localPosition = Vector3.Lerp(start_pos, dest_pos, 1.0f - (anim_time / DIRECTION_ADJUSTMENT_TIME));

            yield return null;
        }

        displayTorpedoDirectionAdjustment();

        BUTTON_LISTS[1][0].untoggle();
        BUTTON_LISTS[1][1].untoggle();
        BUTTON_LISTS[1][0].updateInteractable(current_torpedo_bay > 0 && is_powered);
        BUTTON_LISTS[1][1].updateInteractable(current_torpedo_bay < 3 && is_powered);

        torpedo_direction_adjustment_coroutine = null;

        BUTTON_LISTS[2][0].updateInteractable(getCurrentlyLoadable());
    }

    //turns the dial to confirm load of a torpedo
    IEnumerator torpedoLoadConfirmation(int torpedo_bay, int torpedo_type)
    {
        float destination_rotation = 90.0f;

        deactivateButtons();
        displayTorpedoSelectionAdjustment();

        float anim_time = LOAD_CONFIRMATION_TIME;
        for (int i = 0; i <= 1; i++)
        {
            float half_time = LOAD_CONFIRMATION_TIME * 0.5f;
            float curr_time = half_time;

            while (curr_time > 0.0f)
            {
                curr_time = Mathf.Max(0.0f, curr_time - Time.deltaTime);

                float switch_percentage = 1.0f - (curr_time / half_time);
                if (i == 1)
                {
                    switch_percentage = (curr_time / half_time);
                }

                torpedo_confirmation_switch.transform.localRotation = Quaternion.Euler(-54.0f, 315.0f, Mathf.Lerp(0.0f, destination_rotation, switch_percentage));

                yield return null;
            }

            if (i == 0)
            {
                Texture torpedo_icon = engineer_inventory.getItemTexture(1, torpedo_type);
                Color torpedo_color = engineer_inventory.getItemColor(1, torpedo_type);
                torpedo_color = new Color(torpedo_color.r, torpedo_color.g, torpedo_color.b, 1.0f);

                //update both torpedo loader display and ship overview display
                GameObject[] to_update = new GameObject[2] { torpedo_direction_display, ship_overview_torpedo_information };
                for (int x = 0; x < 2; x++)
                {
                    to_update[x].transform.GetChild(torpedo_bay).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().texture = torpedo_icon;
                    to_update[x].transform.GetChild(torpedo_bay).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = torpedo_color;
                    to_update[x].transform.GetChild(torpedo_bay).GetChild(0).gameObject.SetActive(true);
                }

                //show update to torpedo loader display
                displayTorpedoDirectionAdjustment();
            }
        }

        torpedo_confirmation_coroutine = null;

        BUTTON_LISTS[0][0].updateInteractable(is_powered);
        BUTTON_LISTS[0][1].updateInteractable(is_powered);

        BUTTON_LISTS[1][0].updateInteractable(current_torpedo_bay > 0 && is_powered);
        BUTTON_LISTS[1][1].updateInteractable(current_torpedo_bay < 3 && is_powered);

        BUTTON_LISTS[2][0].updateInteractable(getCurrentlyLoadable());
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false || torpedo_confirmation_coroutine != null)
        {
            return;
        }

        int ray_target_index = ray_targets.IndexOf(current_target.name);

        if (ray_target_index == 0) //torpedo type
        {
            if (torpedo_selection_adjustment_coroutine == null)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //left
                {
                    BUTTON_LISTS[0][0].toggle();
                    BUTTON_LISTS[0][1].updateInteractable(false);
                    current_torpedo_selection -= 1;
                    if (current_torpedo_selection < 0)
                    {
                        current_torpedo_selection = engineer_inventory.getNumberOfItemVariations(1) - 1;
                    }
                    transmitTorpedoSelectionAdjustmentRPC(current_torpedo_selection, true);
                }
                else if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //right
                {
                    BUTTON_LISTS[0][1].toggle();
                    BUTTON_LISTS[0][0].updateInteractable(false);
                    current_torpedo_selection += 1;
                    if (current_torpedo_selection > engineer_inventory.getNumberOfItemVariations(1) - 1)
                    {
                        current_torpedo_selection = 0;
                    }
                    transmitTorpedoSelectionAdjustmentRPC(current_torpedo_selection, false);
                }
            }
        }
        else if (ray_target_index == 1) //torpedo bay
        {
            if (torpedo_direction_adjustment_coroutine == null)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && current_torpedo_bay > 0) //left
                {
                    BUTTON_LISTS[1][0].toggle();
                    BUTTON_LISTS[1][1].updateInteractable(false);
                    current_torpedo_bay -= 1;
                    transmitTorpedoBayDirectionAdjustmentRPC(current_torpedo_bay);
                }
                else if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs) && current_torpedo_bay < 3) //right
                {
                    BUTTON_LISTS[1][1].toggle();
                    BUTTON_LISTS[1][0].updateInteractable(false);
                    current_torpedo_bay += 1;
                    transmitTorpedoBayDirectionAdjustmentRPC(current_torpedo_bay);
                }
            }
        }
        else //confirm load
        {
            if (getCurrentlyLoadable() == true)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[2], inputs)) //confirm load
                {
                    BUTTON_LISTS[2][0].toggle(0.2f);
                    BUTTON_LISTS[2][0].updateInteractable(false);
                    transmitTorpedoLoadConfirmationRPC(current_torpedo_bay, current_torpedo_selection);
                }
            }
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;

        current_torpedo_selection = 0;

        displayTorpedoSelectionAdjustment();

        BUTTON_LISTS[0][0].updateInteractable(true);
        BUTTON_LISTS[0][1].updateInteractable(true);

        BUTTON_LISTS[1][0].updateInteractable(current_torpedo_bay > 0);
        BUTTON_LISTS[1][1].updateInteractable(current_torpedo_bay < 3);

        BUTTON_LISTS[2][0].updateInteractable(getCurrentlyLoadable());

        torpedo_selection_display.SetActive(true);
        torpedo_direction_display.SetActive(true);
    }

    private void deactivateButtons()
    {
        for (int i = 0; i < 2; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
        }

        BUTTON_LISTS[2][0].updateInteractable(false);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;

        torpedo_confirmation_switch.transform.GetChild(0).GetComponent<Renderer>().material = unlit_neon;

        deactivateButtons();

        torpedo_selection_display.SetActive(false);
        torpedo_direction_display.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTorpedoSelectionAdjustmentRPC(int new_selection, bool left)
    {
        if (torpedo_selection_adjustment_coroutine != null)
        {
            StopCoroutine(torpedo_selection_adjustment_coroutine);
        }
        
        current_torpedo_selection = new_selection;

        torpedo_selection_adjustment_coroutine = StartCoroutine(torpedoSelectionAdjustment(left));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTorpedoBayDirectionAdjustmentRPC(int new_bay)
    {
        if (torpedo_direction_adjustment_coroutine != null)
        {
            StopCoroutine(torpedo_direction_adjustment_coroutine);
        }

        current_torpedo_bay = new_bay;

        torpedo_direction_adjustment_coroutine = StartCoroutine(torpedoDirectionAdjustment());
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTorpedoLoadConfirmationRPC(int torpedo_bay, int torpedo_selection)
    {
        torpedo_bay_slots[torpedo_bay] = torpedo_selection;

        engineer_inventory.removeItem(1, torpedo_selection);

        if (torpedo_confirmation_coroutine != null)
        {
            StopCoroutine(torpedo_confirmation_coroutine);
        }

        torpedo_confirmation_coroutine = StartCoroutine(torpedoLoadConfirmation(torpedo_bay, torpedo_selection));
    }
}