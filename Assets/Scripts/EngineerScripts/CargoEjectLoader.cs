/*
    CargoEjectLoader.cs
    - Handles the loading and unloading of items in the cargo eject launcher
    Contributor(s): Jake Schott
    Last Updated: 9/27/2025
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CargoEjectLoader : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SELECTION_ADJUSTMENT_TIME = 0.25f;
    private static float ITEM_TYPE_ADJUSTMENT_TIME = 0.5f;
    private static float LOAD_CONFIRMATION_TIME = 1.0f;

    private string[] CONTROL_NAMES = new string[] { "CARGO EJECT ITEM TYPE SELECTOR", "CARGO EJECT ITEM VARIATION", "CARGO EJECT LOADER" };
    private List<string> CONTROL_DESCS = new List<string> { "SWITCH", "SELECT LEFT", "SELECT RIGHT", "LOAD", "UNLOAD" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6, 4, 5, 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject cargo_eject_load_display;

    public GameObject cargo_eject_item_type_switch;
    public GameObject cargo_eject_item_variation_switch;
    public GameObject cargo_eject_load_dial;

    private EngineerInventory engineer_inventory;

    private bool is_powered = false;
    private int item_type_category = 0;
    private int item_variation_index = 0;
    private bool item_loaded = false;
    private Vector3 item_type_switch_initial_position;
    private Vector3 item_type_switch_direction = new Vector3(-0.0182f, 0.0f, -0.0182f);
    private Coroutine item_type_adjustment_coroutine = null;
    private Coroutine item_variation_adjustment_coroutine = null;
    private Coroutine cargo_eject_load_confirmation_coroutine = null;

    private List<string> ray_targets = new List<string> { "cargo_eject_load_item_type", "cargo_eject_load_item_selector", "cargo_eject_load_dial" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        engineer_inventory = GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerInventory>();

        item_type_switch_initial_position = cargo_eject_item_type_switch.transform.localPosition;

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, true));

        BUTTON_LISTS[2].Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[3], false, true));

        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        hud_info.setButtons(BUTTON_LISTS[0], 6);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);

        hud_info.setTitle(CONTROL_NAMES[index]);

        if (index == 1)
        {
            hud_info.setButtons(BUTTON_LISTS[index], 7);
        }
        else
        {
            hud_info.setButtons(BUTTON_LISTS[index], 6);
        }

        return hud_info;
    }

    private void displayAdjustment(bool loading)
    {
        UnityEngine.UI.RawImage item_icon = cargo_eject_load_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>();
        TMP_Text item_text = cargo_eject_load_display.transform.GetChild(0).GetComponent<TMP_Text>();
        TMP_Text quantity_text = cargo_eject_load_display.transform.GetChild(2).GetComponent<TMP_Text>();

        Color item_color = engineer_inventory.getItemColor(item_type_category, item_variation_index);

        //make transparent if none available
        float a = 1.0f;
        if (engineer_inventory.getItemQuantity(item_type_category, item_variation_index) <= 0 || item_loaded == true || loading == true)
        {
            a = 0.2f;
        }
        item_color = new Color(item_color.r, item_color.g, item_color.b, a);

        //set title text
        item_text.color = item_color;
        string item_title = engineer_inventory.getItemName(item_type_category, item_variation_index).ToUpper();
        if (item_type_category == 1)
        {
            item_title += " TORPEDO";
        }
        item_text.SetText(item_title);

        //set icon
        item_icon.color = item_color;
        item_icon.texture = engineer_inventory.getItemTexture(item_type_category, item_variation_index);

        //set quantity text
        quantity_text.color = item_color;
        if (loading == true)
        {
            quantity_text.SetText("ITEM LOADING");
        }
        else if (item_loaded == true)
        {
            quantity_text.SetText("ITEM LOADED");
        }
        else
        {
            string item_quantity = "QUANTITY: " + engineer_inventory.getItemQuantity(item_type_category, item_variation_index);
            quantity_text.SetText(item_quantity);
        }
    }

    //handles the switch between normal/torpedo items
    IEnumerator itemCategoryAdjustment()
    {
        deactivateButtons();

        Vector3 start_pos = cargo_eject_item_type_switch.transform.localPosition;
        Vector3 dest_pos = item_type_switch_initial_position;

        if (item_type_category == 1)
        {
            dest_pos += item_type_switch_direction;
        }

        float anim_time = ITEM_TYPE_ADJUSTMENT_TIME;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            cargo_eject_item_type_switch.transform.localPosition = Vector3.Lerp(start_pos, dest_pos, 1.0f - (anim_time / ITEM_TYPE_ADJUSTMENT_TIME));

            yield return null;
        }

        displayAdjustment(false);

        BUTTON_LISTS[0][0].untoggle();

        item_type_adjustment_coroutine = null;

        activateButtons();
    }

    //handles the left-right switch that switches between items
    IEnumerator itemVariationAdjustment(bool left)
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

                cargo_eject_item_variation_switch.transform.localRotation = Quaternion.Euler(0.0f, Mathf.Lerp(0.0f, destination_rotation, switch_percentage), 90.0f);

                yield return null;
            }

            if (i == 0)
            {
                displayAdjustment(false);
            }
        }

        BUTTON_LISTS[1][0].untoggle();
        BUTTON_LISTS[1][1].untoggle();

        item_variation_adjustment_coroutine = null;

        activateButtons();
    }

    //turns the dial to confirm cargo eject load of an item
    IEnumerator itemLoadConfirmation(bool is_loading)
    {
        float destination_rotation = 90.0f;
        float start_rotation = 0.0f;

        deactivateButtons();
        displayAdjustment(is_loading);

        if (is_loading == false)
        {
            destination_rotation = 0.0f;
            start_rotation = 90.0f;
        }

        float anim_time = LOAD_CONFIRMATION_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            cargo_eject_load_dial.transform.localRotation = Quaternion.Euler(-54.0f, 315.0f, Mathf.Lerp(start_rotation, destination_rotation, 1.0f - (anim_time / LOAD_CONFIRMATION_TIME)));

            yield return null;
        }

        if (is_loading == true)
        {
            BUTTON_LISTS[2][0].updateDesc(CONTROL_DESCS[4]);
        }
        else
        {
            BUTTON_LISTS[2][0].updateDesc(CONTROL_DESCS[3]);
        }

        item_loaded = is_loading;
        cargo_eject_load_confirmation_coroutine = null;

        displayAdjustment(false);
        activateButtons();
    }

    private bool getCurrentlyLoadable()
    {
        if (item_loaded == true)
        {
            return false;
        }

        if (engineer_inventory.getItemQuantity(item_type_category, item_variation_index) <= 0)
        {
            return false;
        }

        if (item_type_adjustment_coroutine != null || item_variation_adjustment_coroutine != null || cargo_eject_load_confirmation_coroutine != null)
        {
            return false;
        }

        return true;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        if (item_type_adjustment_coroutine != null || item_variation_adjustment_coroutine != null || cargo_eject_load_confirmation_coroutine != null)
        {
            return;
        }

        int ray_target_index = ray_targets.IndexOf(current_target.name);

        if (ray_target_index == 0) //item type
        {
            if (item_loaded == false)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //switch
                {
                    BUTTON_LISTS[0][0].toggle();
                    BUTTON_LISTS[0][0].updateInteractable(false);
                    if (item_type_category == 0)
                    {
                        item_type_category = 1;
                    }
                    else
                    {
                        item_type_category = 0;
                    }
                    transmitItemCategorySwitchAdjustmentRPC(item_type_category);
                }
            }
        }
        else if (ray_target_index == 1) //item variation
        {
            if (item_loaded == false)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //left
                {
                    BUTTON_LISTS[1][0].toggle();
                    BUTTON_LISTS[1][1].updateInteractable(false);
                    item_variation_index -= 1;
                    if (item_variation_index < 0)
                    {
                        item_variation_index = engineer_inventory.getNumberOfItemVariations(item_type_category) - 1;
                    }
                    transmitItemVariationSwitchAdjustmentRPC(item_type_category, item_variation_index, true);
                }
                else if (ControlScript.checkInputIndex(CONTROL_INDEXES[2], inputs)) //right
                {
                    BUTTON_LISTS[1][1].toggle();
                    BUTTON_LISTS[1][0].updateInteractable(false);
                    item_variation_index += 1;
                    if (item_variation_index > engineer_inventory.getNumberOfItemVariations(item_type_category) - 1)
                    {
                        item_variation_index = 0;
                    }
                    transmitItemVariationSwitchAdjustmentRPC(item_type_category, item_variation_index, false);
                }
            }
        }
        else //confirm load/unload
        {
            if (getCurrentlyLoadable() || item_loaded == true)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[3], inputs)) 
                {
                    BUTTON_LISTS[2][0].toggle(0.2f);
                    BUTTON_LISTS[2][0].updateInteractable(false);
                    transmitCargoLoadChangeRPC(item_type_category, item_variation_index, !item_loaded);
                }
            }
        }
    }

    private void activateButtons()
    {
        BUTTON_LISTS[0][0].updateInteractable(item_loaded == false);

        BUTTON_LISTS[1][0].updateInteractable(item_loaded == false);
        BUTTON_LISTS[1][1].updateInteractable(item_loaded == false);

        BUTTON_LISTS[2][0].updateInteractable(getCurrentlyLoadable() || item_loaded == true);
    }

    private void deactivateButtons()
    {
        BUTTON_LISTS[0][0].updateInteractable(false);

        BUTTON_LISTS[1][0].updateInteractable(false);
        BUTTON_LISTS[1][1].updateInteractable(false);

        BUTTON_LISTS[2][0].updateInteractable(false);
    }

    public void powerOn(int position)
    {
        is_powered = true;

        item_type_category = 0;
        item_variation_index = 0;

        displayAdjustment(false);
        activateButtons();

        cargo_eject_load_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;

        deactivateButtons();

        cargo_eject_load_display.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitItemCategorySwitchAdjustmentRPC(int itc)
    {
        if (item_type_adjustment_coroutine != null)
        {
            StopCoroutine(item_type_adjustment_coroutine);
        }

        item_type_category = itc;
        item_variation_index = 0;

        item_type_adjustment_coroutine = StartCoroutine(itemCategoryAdjustment());
    }

    [Rpc(SendTo.Everyone)]
    private void transmitItemVariationSwitchAdjustmentRPC(int itc, int ivi, bool left)
    {
        if (item_variation_adjustment_coroutine != null)
        {
            StopCoroutine(item_variation_adjustment_coroutine);
        }

        item_type_category = itc;
        item_variation_index = ivi;

        item_variation_adjustment_coroutine = StartCoroutine(itemVariationAdjustment(left));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitCargoLoadChangeRPC(int itc, int ivi, bool load)
    {
        if (cargo_eject_load_confirmation_coroutine != null)
        {
            StopCoroutine(cargo_eject_load_confirmation_coroutine);
        }

        item_type_category = itc;
        item_variation_index = ivi;

        if (load == true)
        {
            engineer_inventory.removeItem(item_type_category, item_variation_index);
        }
        else
        {
            engineer_inventory.addItem(item_type_category, item_variation_index);
        }

        cargo_eject_load_confirmation_coroutine = StartCoroutine(itemLoadConfirmation(load));
    }
}