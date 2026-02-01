/*
    CargoEjectLoader.cs
    - Handles the loading and unloading of items in the cargo eject launcher
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
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
    private static float LOAD_CONFIRMATION_TIME = 0.8f;
    private static Vector3 ITEM_TYPE_SWITCH_DIRECTION = new Vector3(-0.0182f, 0.0f, -0.0182f);

    private string[] CONTROL_NAMES = new string[] { "CARGO EJECT ITEM TYPE SELECTOR", "CARGO EJECT ITEM VARIATION", "CARGO EJECT LOADER" };
    private List<string> INFO_MESSAGES = new List<string>() { "Switches between normal items and torpedoes.", "Selects which item to load into cargo eject bay.", "Loads and unloads item from cargo eject bay." };
    private List<string> CONTROL_DESCS = new List<string> { "SWITCH", "SELECT LEFT", "SELECT RIGHT", "LOAD", "UNLOAD" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6, 4, 5, 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject cargo_eject_load_display;

    public GameObject cargo_eject_item_type_switch;
    public GameObject cargo_eject_item_variation_switch;
    public GameObject cargo_eject_load_dial;

    private ShipInventory ship_inventory;
    private CargoEject cargo_eject;

    private bool is_powered = false;
    private int item_type_category = 0;
    private int item_variation_index = 0;
    private bool item_loaded = false;
    private bool item_ejecting = false;
    private string item_serial_num = "";
    private Vector3 item_type_switch_initial_position;
    private Coroutine item_type_adjustment_coroutine = null;
    private Coroutine item_variation_adjustment_coroutine = null;
    private Coroutine cargo_eject_load_confirmation_coroutine = null;

    private List<string> ray_targets = new List<string> { "cargo_eject_load_item_type", "cargo_eject_load_item_selector", "cargo_eject_load_dial" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        ship_inventory = GameObject.FindGameObjectWithTag("Spaceship").GetComponent<ShipInventory>();
        cargo_eject = ReferenceAssistor.Instance.module_handlers[3].GetComponent<CargoEject>();

        item_type_switch_initial_position = cargo_eject_item_type_switch.transform.localPosition;

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, true));

        BUTTON_LISTS[2].Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[3], false, true));

        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        hud_info.setButtons(BUTTON_LISTS[0], 6);
        hud_info.setInfo(INFO_MESSAGES[0]);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);

        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setInfo(INFO_MESSAGES[index]);

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

    public void resetToDefault()
    {
        //stop all coroutines
        StopAllCoroutines();

        if (item_loaded == true)
        {
            //reset switches/dial
            cargo_eject_item_type_switch.transform.localPosition = item_type_switch_initial_position;
            cargo_eject_load_dial.transform.localRotation = Quaternion.Euler(-54.0f, 315.0f, 0.0f);
            cargo_eject_item_variation_switch.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);

            //unload item
            if (NetworkManager.Singleton.IsHost == true)
            {
                transmitCargoLoadChangeRPC(item_type_category, item_variation_index, false);
            }
        }

        item_type_category = 0;
        item_variation_index = 0;
        displayAdjustment(false);
        cargo_eject.deactivate();
    }

    public Texture getCurrentItemImage()
    {
        return ship_inventory.getItemTexture(item_type_category, item_variation_index);
    }

    public Color getCurrentItemColor()
    {
        return ship_inventory.getItemColor(item_type_category, item_variation_index);
    }

    public string getCurrentItemSerialNumber()
    {
        return item_serial_num;
    }

    public int getEjectItemIndex()
    {
        return (item_type_category * ShipInventory.ITEM_NAMES.Count) + item_variation_index;
    }

    public void onInventoryChange()
    {
        displayAdjustment(cargo_eject_load_confirmation_coroutine != null);
        if (is_powered == true)
        {
            if (cargo_eject_load_confirmation_coroutine == null && item_type_adjustment_coroutine == null && item_variation_adjustment_coroutine == null)
            {
                activateButtons();
            }
        }
    }

    private void displayAdjustment(bool adjusting)
    {
        string name_of_item = ship_inventory.getItemName(item_type_category, item_variation_index);

        TMP_Text item_name = cargo_eject_load_display.transform.GetChild(0).GetComponent<TMP_Text>();
        TMP_Text item_id = cargo_eject_load_display.transform.GetChild(1).GetComponent<TMP_Text>();
        UnityEngine.UI.RawImage item_icon = cargo_eject_load_display.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>();
        TMP_Text item_info = cargo_eject_load_display.transform.GetChild(3).GetComponent<TMP_Text>();
        TMP_Text quantity_text = cargo_eject_load_display.transform.GetChild(4).GetComponent<TMP_Text>();

        Color item_color = ship_inventory.getItemColor(item_type_category, item_variation_index);

        //make transparent if none available/loading
        float a = 1.0f;
        if (ship_inventory.getItemQuantity(item_type_category, item_variation_index) <= 0 || item_loaded == true || adjusting == true)
        {
            a = 0.2f;
        }
        item_color = new Color(item_color.r, item_color.g, item_color.b, a);

        //set title text
        item_name.color = item_color;
        string item_title = name_of_item.ToUpper();
        if (item_type_category == 1)
        {
            item_title += " TORPEDO";
        }
        item_name.SetText(item_title);

        //set id text
        item_id.color = item_color;
        item_id.SetText("ITEM ID: " + ship_inventory.getItemID(name_of_item));

        //set icon
        item_icon.color = item_color;
        item_icon.texture = ship_inventory.getItemTexture(item_type_category, item_variation_index);

        //set item info
        item_info.color = item_color;
        Vector2 item_size = ship_inventory.getItemSize(name_of_item);
        item_info.SetText("WEIGHT: " + ship_inventory.getItemWeight(name_of_item) + "kg\nHEIGHT: " + item_size.x + "m\nLENGTH: " + item_size.y + "m");

        //change bar colors
        foreach (Transform bar in cargo_eject_load_display.transform.GetChild(6))
        {
            bar.GetComponent<UnityEngine.UI.RawImage>().color = item_color;
        }
        cargo_eject_load_display.transform.GetChild(5).GetComponent<UnityEngine.UI.Image>().color = item_color;

        //set quantity text
        quantity_text.color = item_color;
        if (adjusting == true && item_loaded == false)
        {
            quantity_text.SetText("ITEM LOADING");
        }
        else if (item_ejecting == false && item_loaded == true)
        {
            quantity_text.SetText("ITEM LOADED");
        }
        else if (adjusting == true && item_ejecting == true)
        {
            quantity_text.SetText("ITEM EJECTED");
        }
        else
        {
            string item_quantity = "QUANTITY: " + ship_inventory.getItemQuantity(item_type_category, item_variation_index);
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
            dest_pos += ITEM_TYPE_SWITCH_DIRECTION;
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
        displayAdjustment(true);

        if (is_loading == false)
        {
            destination_rotation = 0.0f;
            start_rotation = 90.0f;
        }

        float anim_time = LOAD_CONFIRMATION_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            //turn dial
            cargo_eject_load_dial.transform.localRotation = Quaternion.Euler(-54.0f, 315.0f, Mathf.Lerp(start_rotation, destination_rotation, 1.0f - (anim_time / LOAD_CONFIRMATION_TIME)));

            //update fill bar
            if (is_loading == false)
            {
                cargo_eject_load_display.transform.GetChild(5).GetComponent<UnityEngine.UI.Image>().fillAmount = anim_time / LOAD_CONFIRMATION_TIME;
            }
            else
            {
                cargo_eject_load_display.transform.GetChild(5).GetComponent<UnityEngine.UI.Image>().fillAmount = 1.0f - (anim_time / LOAD_CONFIRMATION_TIME);
            }

            yield return null;
        }

        if (is_loading == true)
        {
            cargo_eject.activate();
            BUTTON_LISTS[2][0].updateDesc(CONTROL_DESCS[4]);
        }
        else
        {
            cargo_eject.deactivate();
            BUTTON_LISTS[2][0].updateDesc(CONTROL_DESCS[3]);
        }

        item_loaded = is_loading;
        item_ejecting = false;
        cargo_eject_load_confirmation_coroutine = null;

        displayAdjustment(false);
        activateButtons();
    }

    //called by CargoEject on cargo ejection
    public void onCargoEject()
    {
        item_ejecting = true;

        if (cargo_eject_load_confirmation_coroutine != null)
        {
            StopCoroutine(cargo_eject_load_confirmation_coroutine);
        }

        cargo_eject_load_confirmation_coroutine = StartCoroutine(itemLoadConfirmation(false));
    }

    private bool getCurrentlyLoadable()
    {
        if (item_loaded == true)
        {
            return false;
        }

        if (ship_inventory.getItemQuantity(item_type_category, item_variation_index) <= 0)
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
                        item_variation_index = ship_inventory.getNumberOfItemVariations(item_type_category) - 1;
                    }
                    transmitItemVariationSwitchAdjustmentRPC(item_type_category, item_variation_index, true);
                }
                else if (ControlScript.checkInputIndex(CONTROL_INDEXES[2], inputs)) //right
                {
                    BUTTON_LISTS[1][1].toggle();
                    BUTTON_LISTS[1][0].updateInteractable(false);
                    item_variation_index += 1;
                    if (item_variation_index > ship_inventory.getNumberOfItemVariations(item_type_category) - 1)
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

        if (item_loaded == false)
        {
            item_type_category = 0;
            item_variation_index = 0;

            displayAdjustment(false);
        }

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

        if (NetworkManager.Singleton.IsHost == true)
        {
            if (load == true)
            {
                item_serial_num = ship_inventory.removeItem(item_type_category, item_variation_index);
            }
            else
            {
                ship_inventory.addItem(item_type_category, item_variation_index, item_serial_num);
            }
        }

        cargo_eject_load_confirmation_coroutine = StartCoroutine(itemLoadConfirmation(load));
    }
}