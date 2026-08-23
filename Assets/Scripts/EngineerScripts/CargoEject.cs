/*
    CargoEject.cs
    - Handles selecting and ejecting of items
    Contributor(s): Jake Schott
    Last Updated: 8/23/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CargoEject : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float SELECTION_ADJUSTMENT_TIME = 0.25f;
    private static float ITEM_TYPE_ADJUSTMENT_TIME = 0.5f;
    private static float EJECT_CONFIRMATION_TIME = 0.8f;
    private static float CARGO_TRANSFORM_ADJUSTMENT_TIME = 3.0f;
    private static Vector3 ITEM_TYPE_SWITCH_DIRECTION = new Vector3(-0.0182f, 0.0f, -0.0182f);
    private static float[] SPAWN_X_COORDINATES = new float[] { -3.3f, 3.3f }; //cargo spawn positions so they don't bump into each other

    private string[] CONTROL_NAMES = new string[] { "CARGO TYPE SELECTOR", "CARGO ITEM VARIATION", "CARGO EJECTOR" };
    private List<string> INFO_MESSAGES = new List<string>() { "Switches between utility items and torpedoes.", "Selects which item variation will be ejected.", "Ejects selected item into outer space." };
    private List<string> CONTROL_DESCS = new List<string> { "SWITCH", "SELECT LEFT", "SELECT RIGHT", "EJECT" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6, 4, 5, 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject cargo_eject_display;
    public GameObject cargo_eject_item_type_switch;
    public GameObject cargo_eject_item_variation_switch;
    public GameObject cargo_eject_dial;
    public ShipExteriorFeatures ship_exterior_features;
    public AudioSource cargo_eject_boop_sound;
    public List<AudioSource> cargo_eject_sounds = null;
    private TMP_Text cargo_eject_description_text;
    private UnityEngine.UI.Image cargo_eject_fill_bar;
    private ShipInventory ship_inventory;

    private bool is_powered = false;
    private int spawn_index = 0; //either 0 or 1 (corresponds to SPAWN_X_COORDINATES)
    private float last_spawned_distance = 120.0f; //used to prevent collisions on laucnh
    private int item_type_category = 0;
    private int item_variation_index = 0;
    private string item_serial_num = "";
    private float item_eject_percentage = 0.0f;
    private Vector3 item_type_switch_initial_position;
    private Coroutine item_type_adjustment_coroutine = null;
    private Coroutine item_variation_adjustment_coroutine = null;
    private Coroutine item_eject_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private List<string> ray_targets = new List<string> { "cargo_eject_type_switch", "cargo_eject_variation_selector", "cargo_eject_dial" };

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public List<AnimatorHandler.HandInteractionType> hand_interaction_types = null;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private int my_control_index = 0;

    private void Start()
    {
        ship_inventory = GameObject.FindGameObjectWithTag("Spaceship").GetComponent<ShipInventory>();
        cargo_eject_description_text = cargo_eject_display.transform.GetChild(4).GetComponent<TMP_Text>();
        cargo_eject_fill_bar = cargo_eject_display.transform.GetChild(5).GetComponent<UnityEngine.UI.Image>();

        item_type_switch_initial_position = cargo_eject_item_type_switch.transform.localPosition;

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, true));

        BUTTON_LISTS[2].Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[3], false, false));

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

    public Transform getIKTarget(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        my_control_index = index;
        return IK_targets[index].transform;
    }

    public AnimatorHandler.HandInteractionType getHandInteractionType()
    {
        return hand_interaction_types[my_control_index];
    }

    public float getHandPose()
    {
        return hand_pose;
    }

    public bool getRightHandFlip()
    {
        return does_right_hand_flip;
    }
    public Vector3 getRightHandOffset()
    {
        return right_hand_offset;
    }
    public float getLerpSpeed()
    {
        return lerp_speed;
    }

    public void resetToDefault()
    {
        //stop all coroutines
        StopAllCoroutines();

        //reset switches/dial
        cargo_eject_item_type_switch.transform.localPosition = item_type_switch_initial_position;
        cargo_eject_dial.transform.localRotation = Quaternion.Euler(-54.0f, 315.0f, 0.0f);
        cargo_eject_item_variation_switch.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);

        item_type_category = 0;
        item_variation_index = 0;
        cargo_eject_description_text.SetText("LOADING");
        cargo_eject_fill_bar.fillAmount = 0.0f;
        displayAdjustment(false);
    }

    private int getEjectItemIndex()
    {
        return (item_type_category * ShipInventory.ITEM_NAMES.Count) + item_variation_index;
    }

    public void onInventoryChange()
    {
        displayAdjustment(item_eject_coroutine != null || ship_inventory.getItemQuantity(item_type_category, item_variation_index) == 0);
        if (is_powered == true)
        {
            if (item_eject_coroutine == null && item_type_adjustment_coroutine == null && item_variation_adjustment_coroutine == null)
            {
                activateButtons(true);
            }
        }
    }

    private void displayAdjustment(bool transparent)
    {
        string name_of_item = ship_inventory.getItemName(item_type_category, item_variation_index);
        Color item_color = ship_inventory.getItemColor(item_type_category, item_variation_index);

        TMP_Text item_name = cargo_eject_display.transform.GetChild(0).GetComponent<TMP_Text>();
        UnityEngine.UI.RawImage item_icon = cargo_eject_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>();
        TMP_Text item_counter = cargo_eject_display.transform.GetChild(2).GetChild(1).GetComponent<TMP_Text>();
        TMP_Text item_info = cargo_eject_display.transform.GetChild(3).GetComponent<TMP_Text>();

        //make transparent if none available/loading
        float a = 1.0f;
        if (transparent == true)
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

        //set icon
        item_icon.color = item_color;
        item_icon.texture = ship_inventory.getItemTexture(item_type_category, item_variation_index);

        //set item info
        item_info.color = item_color;
        Vector2 item_size = ship_inventory.getItemSize(name_of_item);
        item_info.SetText("ITEM ID: " + ship_inventory.getItemID(name_of_item) + "\nWEIGHT: " + ship_inventory.getItemWeight(name_of_item) + "kg\nHEIGHT: " + item_size.x + "m\nLENGTH: " + item_size.y + "m");

        //set item counter color
        item_counter.color = item_color;
        item_counter.transform.GetChild(0).GetComponent<TMP_Text>().color = new Color(item_color.r, item_color.g, item_color.b, 0.04f);
        item_counter.transform.parent.GetComponent<UnityEngine.UI.RawImage>().color = item_color;

        //change load bar/text color
        cargo_eject_display.transform.GetChild(4).GetComponent<TMP_Text>().color = new Color(item_color.r, item_color.g, item_color.b, 1.0f);
        cargo_eject_display.transform.GetChild(5).GetComponent<UnityEngine.UI.Image>().color = new Color(item_color.r, item_color.g, item_color.b, 1.0f);
        cargo_eject_display.transform.GetChild(5).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(item_color.r, item_color.g, item_color.b, 0.08f);

        //set item counter text
        string s_available_item = ship_inventory.getItemQuantity(item_type_category, item_variation_index).ToString();
        if (s_available_item.Length == 1)
        {
            s_available_item = "0" + s_available_item;
        }
        else if (s_available_item.Length == 3)
        {
            s_available_item = "99";
        }
        item_counter.SetText(s_available_item);
    }

    private void displayDialTurn()
    {
        cargo_eject_dial.transform.localRotation = Quaternion.Euler(-54.0f, 315.0f, Mathf.Lerp(0.0f, 90.0f, item_eject_percentage));
        cargo_eject_description_text.gameObject.SetActive(item_eject_percentage > 0.0f);
    }

    //handles the switch between normal/torpedo items
    IEnumerator itemCategoryAdjustment()
    {
        deactivateButtons(false);

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

        cargo_eject_boop_sound.Play();
        displayAdjustment(getCurrentlyEjectable() == false);

        BUTTON_LISTS[0][0].untoggle();

        item_type_adjustment_coroutine = null;

        activateButtons(true);
    }

    //handles the left-right switch that switches between items
    IEnumerator itemVariationAdjustment(bool left)
    {
        deactivateButtons(false);

        float destination_rotation = 25.0f;
        if (left == false)
        {
            destination_rotation = -25.0f;
        }

        float anim_time = SELECTION_ADJUSTMENT_TIME;
        for (int i = 0; i < 2; i++)
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
                cargo_eject_boop_sound.Play();
                displayAdjustment(getCurrentlyEjectable() == false);
            }
        }

        BUTTON_LISTS[1][0].untoggle();
        BUTTON_LISTS[1][1].untoggle();

        item_variation_adjustment_coroutine = null;

        activateButtons(true);
    }

    //handles turning of the dial to confirm ejection
    IEnumerator ejectArming()
    {
        deactivateButtons(true);

        while ((keys_down.Count > 0 || item_eject_percentage > 0.0f) && item_eject_percentage < 1.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            float before_eject_percentage = item_eject_percentage;

            bool arming = PrimaryScript.checkInputIndex(CONTROL_INDEXES[3], keys_down);
            if (arming == true && getCurrentlyEjectable() && is_powered == true)
            {
                item_eject_percentage = Mathf.Min(1.0f, ((item_eject_percentage * EJECT_CONFIRMATION_TIME) + dt) / EJECT_CONFIRMATION_TIME);
            }
            else
            {
                item_eject_percentage = Mathf.Max(0.0f, ((item_eject_percentage * EJECT_CONFIRMATION_TIME) - dt) / EJECT_CONFIRMATION_TIME);
            }

            if (item_eject_percentage != before_eject_percentage)
            {
                transmitEjectPercentageRPC(item_eject_percentage);
            }

            keys_down.Clear();
            yield return null;
        }

        item_eject_coroutine = null;

        if (item_eject_percentage >= 1.0f)
        {
            transmitItemLaunchRPC(item_type_category, item_variation_index);
            BUTTON_LISTS[2][0].updateInteractable(false);
        }
        else
        {
            activateButtons(true);
        }
    }

    //run by the host to push the launched cargo item away from the ship
    IEnumerator cargoTransformAdjustment(GameObject ejected_item)
    {
        Transform spaceship = GameObject.FindGameObjectWithTag("Spaceship").transform;

        float anim_time = CARGO_TRANSFORM_ADJUSTMENT_TIME;
        last_spawned_distance -= 5.0f;
        if (last_spawned_distance < 90.0f)
        {
            last_spawned_distance = 120.0f;
        }

        Collider c = ejected_item.GetComponent<Collider>();
        c.excludeLayers = LayerMask.GetMask("ShipColliders");

        while (ejected_item != null && anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            ejected_item.transform.localPosition = new Vector3(ejected_item.transform.localPosition.x, ejected_item.transform.localPosition.y, Mathf.Lerp(last_spawned_distance, 65.0f, anim_time / CARGO_TRANSFORM_ADJUSTMENT_TIME));

            yield return null;
        }

        if (ejected_item != null)
        {
            Transform world_root = GameObject.FindGameObjectWithTag("WorldRoot").transform;
            ejected_item.GetComponent<NetworkObject>().TrySetParent(world_root, true);
            c.excludeLayers = LayerMask.GetMask("None");
        }
    }

    //handles launch of an item
    IEnumerator itemLaunch()
    {
        //open door
        ship_exterior_features.adjustCargoDoorOpen(1, true);

        //spawn item as a NetworkObject if host
        if (NetworkManager.Singleton.IsHost == true)
        {
            spawnAndEjectItem();
        }
        cargo_eject_sounds[spawn_index].Play();

        deactivateButtons(false);
        cargo_eject_description_text.SetText("EJECTING");

        float reset_time = EJECT_CONFIRMATION_TIME * 4.0f;
        float anim_time = reset_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            item_eject_percentage = anim_time / reset_time;

            //turn dial
            displayDialTurn();

            //update fill bar
            cargo_eject_fill_bar.fillAmount = item_eject_percentage;

            yield return null;
        }

        item_eject_coroutine = null;

        activateButtons(true);
        displayAdjustment(getCurrentlyEjectable() == false);
        cargo_eject_description_text.SetText("LOADING");

        //close door
        ship_exterior_features.adjustCargoDoorOpen(1, false);
    }

    //only run by the host
    private void spawnAndEjectItem()
    {
        Transform spaceship = GameObject.FindGameObjectWithTag("Spaceship").transform;
        GameObject ejected_item = GameObject.Instantiate(ReferenceAssistor.Instance.collectible_items[getEjectItemIndex()], spaceship);

        spawn_index = 1 - spawn_index;
        ejected_item.transform.position = spaceship.transform.position + (spaceship.transform.right * SPAWN_X_COORDINATES[spawn_index]) + new Vector3(0.0f, -10.5f, 0.0f) + (spaceship.forward * 65.0f);
        ejected_item.transform.rotation = spaceship.rotation;
        Vector3 curr_rotation = ejected_item.transform.rotation.eulerAngles;
        ejected_item.transform.rotation = Quaternion.Euler(curr_rotation.x + Random.Range(-15.0f, 15.0f), curr_rotation.y + Random.Range(-15.0f, 15.0f), curr_rotation.z + Random.Range(-15.0f, 15.0f));
        ejected_item.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
        ejected_item.GetComponent<NetworkObject>().TrySetParent(spaceship, true);
        ejected_item.GetComponent<CollectibleItem>().setSerialNumber(item_serial_num);
        StartCoroutine(cargoTransformAdjustment(ejected_item));
    }

    private bool getCurrentlyEjectable()
    {
        return (ship_inventory.getItemQuantity(item_type_category, item_variation_index) > 0);
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        keys_down = inputs;

        if (item_type_adjustment_coroutine != null || item_variation_adjustment_coroutine != null || item_eject_coroutine != null)
        {
            return;
        }

        int ray_target_index = ray_targets.IndexOf(current_target.name);

        if (ray_target_index == 0) //item type
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //switch
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
        else if (ray_target_index == 1) //item variation
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //left
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
            else if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[2], inputs)) //right
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
        else //start launch process
        {
            if (getCurrentlyEjectable() == true)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[3], inputs))
                {
                    item_eject_coroutine = StartCoroutine(ejectArming());
                }
            }
        }
    }

    private void activateButtons(bool allow_item_adjustment)
    {
        BUTTON_LISTS[0][0].updateInteractable(is_powered && allow_item_adjustment);

        BUTTON_LISTS[1][0].updateInteractable(is_powered && allow_item_adjustment);
        BUTTON_LISTS[1][1].updateInteractable(is_powered && allow_item_adjustment);

        BUTTON_LISTS[2][0].updateInteractable(is_powered && getCurrentlyEjectable());
    }

    private void deactivateButtons(bool allow_ejecting)
    {
        BUTTON_LISTS[0][0].updateInteractable(false);

        BUTTON_LISTS[1][0].updateInteractable(false);
        BUTTON_LISTS[1][1].updateInteractable(false);

        BUTTON_LISTS[2][0].updateInteractable(is_powered && allow_ejecting && getCurrentlyEjectable());
    }

    public void powerOn(int position)
    {
        is_powered = true;

        item_type_category = 0;
        item_variation_index = 0;

        displayAdjustment(getCurrentlyEjectable() == false);
        activateButtons(true);

        cargo_eject_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;

        deactivateButtons(false);

        cargo_eject_display.SetActive(false);
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
    private void transmitEjectPercentageRPC(float prcnt)
    {
        item_eject_percentage = prcnt;
        displayDialTurn();
        cargo_eject_fill_bar.fillAmount = item_eject_percentage;
    }

    [Rpc(SendTo.Everyone)]
    private void transmitItemLaunchRPC(int itc, int ivi)
    {
        if (item_eject_coroutine != null)
        {
            StopCoroutine(item_eject_coroutine);
        }

        item_type_category = itc;
        item_variation_index = ivi;

        if (NetworkManager.Singleton.IsHost == true)
        {
            item_serial_num = ship_inventory.removeItem(item_type_category, item_variation_index);
        }

        item_eject_coroutine = StartCoroutine(itemLaunch());
    }
}