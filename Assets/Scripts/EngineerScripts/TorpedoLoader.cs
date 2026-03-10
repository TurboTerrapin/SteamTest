/*
    TorpedoLoader.cs
    - Handles the loading of torpedoes 
    Contributor(s): Jake Schott
    Last Updated: 3/8/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class TorpedoLoader : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SELECTION_TURN_TIME = 100.0f;
    private static float DIRECTION_ADJUSTMENT_TIME = 0.2f;
    private static float LOAD_CONFIRMATION_TIME = 0.8f;
    private static Vector3 TORPEDO_BAY_ADJUSTMENT_DIRECTION = new Vector3(0.003f, -0.006f, -0.003f);
    private static Vector3 CONFIRM_BUTTON_DIRECTION = new Vector3(0.002f, -0.004f, -0.002f);

    private string[] CONTROL_NAMES = new string[] { "FORWARD TORPEDO BAY", "PORT TORPEDO BAY", "STARBOARD TORPEDO BAY", "AFT TORPEDO BAY", "TORPEDO SELECTOR", "TORPEDO LOADER" };
    private List<string> INFO_MESSAGES = new List<string>() { "Selects which direction to load into.", "Selects which torpedo to load.", "Loads selected torpedo into corresponding directional bay (cannot be unloaded once loaded)." };
    private List<string> CONTROL_DESCS = new List<string> { "SELECT", "ROTATE LEFT", "ROTATE RIGHT", "CONFIRM" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6, 4, 5, 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject torpedo_loader_display;
    public GameObject ship_overview_torpedo_information;
    private GameObject torpedo_bay_selector_display;

    public GameObject torpedo_selection_dial;
    public GameObject torpedo_direction_buttons;
    public GameObject torpedo_confirm_button;

    private ShipInventory ship_inventory;

    private bool is_powered = false;
    private int[] torpedo_bay_slots = new int[4] { -1, -1, -1, -1 };
    private string[] torpedo_serial_nums = new string[4];
    private int current_torpedo_selection = 0;
    private int current_torpedo_bay = 0;
    private float selection_dial_rotation = 0.0f;
    private Coroutine torpedo_direction_adjustment_coroutine = null;
    private Coroutine torpedo_confirmation_coroutine = null;

    private List<string> ray_targets = new List<string> { "torpedo_loader_direction_forward", "torpedo_loader_direction_port", "torpedo_loader_direction_starboard", "torpedo_loader_direction_aft", "torpedo_loader_selection_dial", "torpedo_loader_confirm_button" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        ship_inventory = GameObject.FindGameObjectWithTag("Spaceship").GetComponent<ShipInventory>();
        torpedo_bay_selector_display = ReferenceAssistor.Instance.module_handlers[1].GetComponent<TorpedoBaySelector>().selector_display;

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, false));

        BUTTON_LISTS[2].Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[3], false, true));

        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        hud_info.setButtons(BUTTON_LISTS[0]);
        hud_info.setInfo(INFO_MESSAGES[0]);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);

        if (index < 4)
        {
            BUTTON_LISTS[0][0].updateInteractable(is_powered && torpedo_confirmation_coroutine == null && torpedo_direction_adjustment_coroutine == null && current_torpedo_bay != index);
            hud_info.setInfo(INFO_MESSAGES[0]);
            hud_info.setButtons(BUTTON_LISTS[0], 6);
        }
        else if (index == 4)
        {
            hud_info.setInfo(INFO_MESSAGES[1]); 
            hud_info.setButtons(BUTTON_LISTS[1]);
        }
        else
        {
            hud_info.setInfo(INFO_MESSAGES[2]);
            hud_info.setButtons(BUTTON_LISTS[2]);
        }

        return hud_info;
    }

    public void onInventoryChange()
    {
        displayTorpedoSelectionAdjustment();
        displayTorpedoDirectionAdjustment();
        updateCurrentlyLoadableIndicators();
        BUTTON_LISTS[2][0].updateInteractable(getCurrentlyLoadable());
    }

    public void unloadTorpedo(int bay)
    {
        torpedo_bay_slots[bay] = -1;
        torpedo_serial_nums[bay] = "";
        displayShipOverviewAdjustment(bay);
        displayTorpedoDirectionAdjustment();
    }
    

    //unloads all torpedoes currently loaded and adds them to ship inventory
    public void resetToDefault()
    {
        int[] torpedoes_to_unload = new int[6];
        Stack<string>[] unloaded_serial_nums = new Stack<string>[6];
        for (int i = 0; i < 6; i++)
        {
            unloaded_serial_nums[i] = new Stack<string>();
        }
        for (int i = 0; i < 4; i++)
        {
            if (torpedo_bay_slots[i] >= 0)
            {
                torpedoes_to_unload[torpedo_bay_slots[i]] += 1;
                unloaded_serial_nums[torpedo_bay_slots[i]].Push(torpedo_serial_nums[i]);
            }
            unloadTorpedo(i);
        }
        if (NetworkManager.Singleton.IsHost == true)
        {
            for (int i = 0; i < 6; i++)
            {
                if (torpedoes_to_unload[i] > 0)
                {
                    ship_inventory.addItems(1, i, unloaded_serial_nums[i]);
                }
            }
        }
    }

    //updates ship overview screen as well as tactician torpedo bay selector screen
    private void displayShipOverviewAdjustment(int bay)
    {
        ship_overview_torpedo_information.transform.GetChild(bay).GetChild(0).gameObject.SetActive(torpedo_bay_slots[bay] == -1);
        ship_overview_torpedo_information.transform.GetChild(bay).GetChild(1).gameObject.SetActive(torpedo_bay_slots[bay] >= 0);
        torpedo_bay_selector_display.transform.GetChild(1).GetChild((bay * 2) + 1).gameObject.SetActive(torpedo_bay_slots[bay] == -1);
        torpedo_bay_selector_display.transform.GetChild(1).GetChild(bay * 2).gameObject.SetActive(torpedo_bay_slots[bay] >= 0);

        if (torpedo_bay_slots[bay] < 0)
        {
            return;
        }

        Texture torpedo_icon = ship_inventory.getItemTexture(1, torpedo_bay_slots[bay]);
        Color torpedo_color = ship_inventory.getItemColor(1, torpedo_bay_slots[bay]);
        torpedo_color = new Color(torpedo_color.r, torpedo_color.g, torpedo_color.b, 1.0f);

        //update icon and color
        ship_overview_torpedo_information.transform.GetChild(bay).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().texture = torpedo_icon;
        ship_overview_torpedo_information.transform.GetChild(bay).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = torpedo_color;
        torpedo_color.a = torpedo_bay_selector_display.transform.GetChild(1).GetChild((bay * 2) + 1).GetComponent<UnityEngine.UI.RawImage>().color.a;
        torpedo_bay_selector_display.transform.GetChild(1).GetChild(bay * 2).GetComponent<UnityEngine.UI.RawImage>().texture = torpedo_icon;
        torpedo_bay_selector_display.transform.GetChild(1).GetChild(bay * 2).GetComponent<UnityEngine.UI.RawImage>().color = torpedo_color;
    }

    private void displayTorpedoSelectionAdjustment()
    {
        UnityEngine.UI.RawImage torpedo_icon = torpedo_loader_display.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>();
        TMP_Text quantity_text = torpedo_loader_display.transform.GetChild(3).GetComponent<TMP_Text>();
        TMP_Text torpedo_text = torpedo_loader_display.transform.GetChild(4).GetComponent<TMP_Text>();

        Color torpedo_color = ship_inventory.getItemColor(1, current_torpedo_selection);

        //adjust circles
        foreach (Transform t in torpedo_loader_display.transform.GetChild(0))
        {
            Color c = t.GetComponent<UnityEngine.UI.RawImage>().color;
            c.a = 0.08f;
            t.GetComponent<UnityEngine.UI.RawImage>().color = c;
        }
        torpedo_color.a = 1.0f;
        torpedo_loader_display.transform.GetChild(0).GetChild(current_torpedo_selection).GetComponent<UnityEngine.UI.RawImage>().color = torpedo_color;
        torpedo_loader_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = torpedo_color;
        torpedo_loader_display.transform.GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = torpedo_color;

        //make transparent if none available
        if (ship_inventory.getItemQuantity(1, current_torpedo_selection) <= 0)
        {
            torpedo_color.a = 0.2f;
        }

        //set icon
        torpedo_icon.color = torpedo_color;
        torpedo_icon.texture = ship_inventory.getItemTexture(1, current_torpedo_selection);

        //set quantity
        string s_torpedo_quantity = ship_inventory.getItemQuantity(1, current_torpedo_selection).ToString();
        if (s_torpedo_quantity.Length == 1)
        {
            s_torpedo_quantity = "0" + s_torpedo_quantity;
        }
        else if (s_torpedo_quantity.Length == 3)
        {
            s_torpedo_quantity = "99";
        }
        quantity_text.SetText(s_torpedo_quantity);
        quantity_text.color = torpedo_color;
        quantity_text.transform.GetChild(0).GetComponent<TMP_Text>().color = new Color(torpedo_color.r, torpedo_color.g, torpedo_color.b, 0.04f);
        for (int i = 0; i < 4; i++)
        {
            quantity_text.transform.GetChild(i + 1).GetComponent<UnityEngine.UI.RawImage>().color = torpedo_color;
        }

        //set text
        torpedo_text.color = torpedo_color;
        torpedo_text.SetText((ship_inventory.getItemName(1, current_torpedo_selection)).ToUpper() + "\nTORPEDO");
    }


    private void displayTorpedoDirectionAdjustment()
    {
        //switch top label/arrow to current bay
        for (int i = 0; i < 4; i++)
        {
            torpedo_loader_display.transform.GetChild(8).GetChild(i).gameObject.SetActive(i == current_torpedo_bay);
        }

        //adjust torpedo in current bay
        torpedo_loader_display.transform.GetChild(6).gameObject.SetActive(torpedo_bay_slots[current_torpedo_bay] == -1);
        torpedo_loader_display.transform.GetChild(7).gameObject.SetActive(torpedo_bay_slots[current_torpedo_bay] != -1);
        if (torpedo_bay_slots[current_torpedo_bay] == -1)
        {
            return;
        }
        Color item_color = ship_inventory.getItemColor(1, torpedo_bay_slots[current_torpedo_bay]);
        item_color.a = 1.0f;
        torpedo_loader_display.transform.GetChild(7).GetComponent<UnityEngine.UI.RawImage>().color = item_color;
        torpedo_loader_display.transform.GetChild(7).GetComponent<UnityEngine.UI.RawImage>().texture = ship_inventory.getItemTexture(1, torpedo_bay_slots[current_torpedo_bay]);
    }

    private void updateCurrentlyLoadableIndicators()
    {
        float a = 1.0f;
        if (getCurrentlyLoadable() == true)
        {
            torpedo_confirm_button.transform.GetChild(0).GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_neon;
        }
        else
        {
            a = 0.08f;
            torpedo_confirm_button.transform.GetChild(0).GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_neon;
        }
        Color c = torpedo_loader_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color;
        c.a = a;
        foreach (Transform t in torpedo_loader_display.transform.GetChild(5))
        {
            t.GetComponent<UnityEngine.UI.RawImage>().color = c;
        }
    }

    //returns true if current torpedo bay is unoccupied, there's at least one torpedo of that type in the inventory, and no coroutines running
    private bool getCurrentlyLoadable()
    {
        if (is_powered == false)
        {
            return false;
        }

        if (ship_inventory.getItemQuantity(1, current_torpedo_selection) <= 0)
        {
            return false;
        }

        if (torpedo_bay_slots[current_torpedo_bay] >= 0)
        {
            return false;
        }

        if (torpedo_direction_adjustment_coroutine != null || torpedo_confirmation_coroutine != null)
        {
            return false;
        }

        return true;
    }

    //returns -1 if unloaded or 0-5 depending on torpedo index
    public int getBayOccupant(int bay)
    {
        return torpedo_bay_slots[bay];
    }

    //handles the push-in buttons that select which bay to load the torpedo in
    IEnumerator torpedoDirectionAdjustment()
    {
        Vector3[] starting_pos = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            starting_pos[i] = torpedo_direction_buttons.transform.GetChild(i).transform.localPosition;
        }

        float anim_time = DIRECTION_ADJUSTMENT_TIME;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            for (int i = 0; i < 4; i++)
            {
                Vector3 dest = Vector3.zero;
                if (i == current_torpedo_bay)
                {
                    dest = TORPEDO_BAY_ADJUSTMENT_DIRECTION;
                }
                torpedo_direction_buttons.transform.GetChild(i).transform.localPosition = Vector3.Lerp(dest, starting_pos[i], anim_time / DIRECTION_ADJUSTMENT_TIME);
            }

            yield return null;
        }

        displayTorpedoDirectionAdjustment();

        torpedo_direction_adjustment_coroutine = null;

        updateCurrentlyLoadableIndicators();
        BUTTON_LISTS[0][0].untoggle();
        BUTTON_LISTS[2][0].updateInteractable(getCurrentlyLoadable());
    }

    //pushes the button to confirm load of a torpedo
    IEnumerator torpedoLoadConfirmation(int torpedo_bay, int torpedo_type)
    {
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

                torpedo_confirm_button.transform.localPosition = Vector3.Lerp(Vector3.zero, CONFIRM_BUTTON_DIRECTION, switch_percentage);

                yield return null;
            }

            if (i == 0)
            {
                //update ship overview screen
                displayShipOverviewAdjustment(torpedo_bay);

                //show update to torpedo loader display
                displayTorpedoDirectionAdjustment();
            }
        }

        torpedo_confirmation_coroutine = null;

        BUTTON_LISTS[1][0].updateInteractable(true);
        BUTTON_LISTS[1][1].updateInteractable(true);

        BUTTON_LISTS[2][0].updateInteractable(getCurrentlyLoadable());
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false || torpedo_confirmation_coroutine != null)
        {
            return;
        }

        int ray_target_index = ray_targets.IndexOf(current_target.name);

        if (ray_target_index < 4) //torpedo type
        {
            if (torpedo_direction_adjustment_coroutine == null &&  torpedo_confirmation_coroutine == null)
            {
                if (ray_target_index == current_torpedo_bay)
                {
                    return;
                }
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //click to select
                {
                    BUTTON_LISTS[0][0].toggle();
                    transmitTorpedoBayDirectionAdjustmentRPC(ray_target_index);
                }
            }
        }
        else if (ray_target_index == 4) //torpedo selection
        {
            if (torpedo_direction_adjustment_coroutine == null)
            {
                int dial_direction = 0;
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[2], inputs)) //E to rotate right
                {
                    dial_direction += 1;
                }
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs))  //Q to rotate left
                {
                    dial_direction -= 1;
                }
                if (dial_direction != 0)
                {
                    if (dial_direction > 0)
                    {
                        selection_dial_rotation += dt * SELECTION_TURN_TIME;
                    }
                    else
                    {
                        selection_dial_rotation -= dt * SELECTION_TURN_TIME;
                    }
                    if (selection_dial_rotation > 360.0f)
                    {
                        selection_dial_rotation -= 360.0f;
                    }
                    else if (selection_dial_rotation < 0.0f)
                    {
                        selection_dial_rotation += 360.0f;
                    }
                    
                    if (selection_dial_rotation > 330.0f)
                    {
                        transmitTorpedoSelectionAdjustmentRPC(selection_dial_rotation, 0);
                    }
                    else
                    {
                        transmitTorpedoSelectionAdjustmentRPC(selection_dial_rotation, Mathf.FloorToInt((selection_dial_rotation + 30.0f) / 60.0f));
                    }
                }
            }
        }
        else //confirm load
        {
            if (getCurrentlyLoadable() == true)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[3], inputs)) //confirm load
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
        selection_dial_rotation = 0.0f;

        BUTTON_LISTS[0][0].updateInteractable(true);

        BUTTON_LISTS[1][0].updateInteractable(true);
        BUTTON_LISTS[1][1].updateInteractable(true);

        BUTTON_LISTS[2][0].updateInteractable(getCurrentlyLoadable());

        displayTorpedoSelectionAdjustment();
        updateCurrentlyLoadableIndicators();

        torpedo_loader_display.SetActive(true);
    }

    private void deactivateButtons()
    {
        BUTTON_LISTS[0][0].updateInteractable(false);

        BUTTON_LISTS[1][0].updateInteractable(false);
        BUTTON_LISTS[1][1].updateInteractable(false);

        BUTTON_LISTS[2][0].updateInteractable(false);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;

        updateCurrentlyLoadableIndicators();
        deactivateButtons();

        torpedo_loader_display.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTorpedoSelectionAdjustmentRPC(float dial_rot, int curr_selection)
    {
        selection_dial_rotation = dial_rot;
        torpedo_selection_dial.transform.localRotation = Quaternion.Euler(-54.0f, -45.0f, dial_rot);
        torpedo_loader_display.transform.GetChild(1).transform.localRotation = Quaternion.Euler(0.0f, 180.0f, dial_rot);

        if (current_torpedo_selection != curr_selection)
        {
            current_torpedo_selection = curr_selection;
            displayTorpedoSelectionAdjustment();
            updateCurrentlyLoadableIndicators();
            BUTTON_LISTS[2][0].updateInteractable(getCurrentlyLoadable());
        }
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

        if (NetworkManager.Singleton.IsHost == true)
        {
            ship_inventory.removeItem(1, torpedo_selection);
        }

        if (torpedo_confirmation_coroutine != null)
        {
            StopCoroutine(torpedo_confirmation_coroutine);
        }
        torpedo_confirmation_coroutine = StartCoroutine(torpedoLoadConfirmation(torpedo_bay, torpedo_selection));
    }
}