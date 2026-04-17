/*
    ShipInventory.cs
    - Handles keeping track of normal items and torpedo items for the whole ship
    - Updates inventory display in engineer position
    - Only the host accepts add/remove/set item queries and passes on to other clients
    Contributor(s): Jake Schott
    Last Updated: 3/13/2026
*/

using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShipInventory : NetworkBehaviour, IPowerable
{
    //CLASS CONSTANTS
    public static List<string> ITEM_NAMES = new List<string>() { "Probe", "Escape Pod", "Shield Battery", "Cargo Container" };
    private static List<int> ITEM_IDS = new List<int>() { 102824, 104308, 102110, 102822 };
    private static List<int> ITEM_WEIGHTS = new List<int>() { 1200, 3500, 500, 5000 };
    private static List<Vector2> ITEM_SIZES = new List<Vector2>() { new Vector2(3.6f, 3.6f), new Vector2(4.2f, 4.8f), new Vector2(3.3f, 4.1f), new Vector2(4.5f, 4.5f) };

    public static List<string> TORPEDO_NAMES = new List<string>() { "Photon", "Proton", "Ion", "Quantum", "Superluminal", "Chroniton" };
    private static List<int> TORPEDO_IDS = new List<int>() { 302025, 302022, 302001, 301995, 301997, 382000 };
    private static List<int> TORPEDO_WEIGHTS = new List<int>() { 5000, 3350, 6000, 1100, 500, 8900 };
    private static List<Vector2> TORPEDO_SIZES = new List<Vector2>() { new Vector2(2.1f, 3.5f), new Vector2(2.1f, 3.5f), new Vector2(2.1f, 3.5f), new Vector2(2.1f, 3.5f), new Vector2(2.1f, 3.5f), new Vector2(2.5f, 4.1f) };

    //based on difficulty (easy, medium, hard, expert [0-3]), determines the starting quantities of items/torpedoes at the very start of a run
    private static int[][] STARTING_QUANTITIES = new int[][]
    {
        new int[]{ 3, 2, 2, 1 }, //probe
        new int[]{ 4, 3, 2, 1 }, //ecape pod
        new int[]{ 12, 10, 8, 4 }, //shield battery
        new int[]{ 4, 3, 2, 1 }, //cargo container
        new int[]{ 8, 6, 4, 2 }, //photon
        new int[]{ 4, 3, 2, 1 }, //proton
        new int[]{ 2, 2, 1, 1 }, //ion
        new int[]{ 2, 1, 0, 0 }, //quantum
        new int[]{ 2, 1, 0, 0 }, //superluminal
        new int[]{ 1, 0, 0, 0 }, //chroniton
    };

    public GameObject inventory_display;

    private ProbeController probe_controller;
    private CargoEjectLoader cargo_eject_loader;
    private ShieldStrength shield_strength;
    private TorpedoLoader torpedo_loader;
    private GameObject item_count_indicators;
    private GameObject torpedo_count_indicators;

    //actual # of items in inventory (Probe, Escape Pod, Shield Battery, Cargo Container)
    private List<int> item_quantities = new List<int>() { 0, 0, 0, 0 };
    //actual # of torpedoes in inventory (Photon, Ion, Proton, Quantum, Superluminal, Chroniton)
    private List<int> torpedo_quantities = new List<int>() { 0, 0, 0, 0, 0, 0 };

    private List<string> used_serial_nums = new List<string>();
    private Stack<string>[] item_serial_nums;
    private Stack<string>[] torpedo_serial_nums;

    private void Start()
    {
        item_count_indicators = inventory_display.transform.GetChild(1).gameObject;
        torpedo_count_indicators = inventory_display.transform.GetChild(2).gameObject;
        probe_controller = ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProbeController>();
        cargo_eject_loader = ReferenceAssistor.Instance.module_handlers[2].GetComponent<CargoEjectLoader>();
        shield_strength = ReferenceAssistor.Instance.module_handlers[2].GetComponent<ShieldStrength>();
        torpedo_loader = ReferenceAssistor.Instance.module_handlers[2].GetComponent<TorpedoLoader>();

        displayAdjustment();
    }

    //generates a new serial code at random
    public string generateSerialNumber()
    {
        string serialNumber = "";
        for (int x = 0; x < 5; x++)
        {
            serialNumber += Random.Range(0, 10) + " ";
        }
        return serialNumber;
    }

    //returns true if a serial number has been occupied
    public bool serialNumberExists(string serial_num)
    {
        for (int i = 0; i < used_serial_nums.Count; i++)
        {
            if (serial_num.CompareTo(used_serial_nums[i]) == 0)
            {
                return true;
            }
        }
        return false;
    }

    //run by host when all clients are loaded in to initialize inventory
    public void initializeInventory()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        int game_difficulty = GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().getDifficulty();
        item_serial_nums = new Stack<string>[item_quantities.Count];
        torpedo_serial_nums = new Stack<string>[torpedo_quantities.Count];

        //initialize items
        for (int i = 0; i < item_quantities.Count; i++)
        {
            item_quantities[i] = STARTING_QUANTITIES[i][game_difficulty];
            item_serial_nums[i] = new Stack<string>();

            for (int x = 0; x < item_quantities[i]; x++)
            {
                bool serial_num_found = false;
                string serial_num = "";
                while (serial_num_found == false)
                {
                    serial_num = generateSerialNumber();
                    serial_num_found = !serialNumberExists(serial_num);
                }
                item_serial_nums[i].Push(serial_num);
                used_serial_nums.Add(serial_num);
            }

            itemInventoryUpdateRPC(0, i, item_quantities[i]);
        }

        //initialize torpedoes
        for (int i = 0; i < torpedo_quantities.Count; i++)
        {
            torpedo_quantities[i] = STARTING_QUANTITIES[i + 4][game_difficulty];
            torpedo_serial_nums[i] = new Stack<string>();

            for (int x = 0; x < torpedo_quantities[i]; x++)
            {
                bool serial_num_found = false;
                string serial_num = "";
                while (serial_num_found == false)
                {
                    serial_num = generateSerialNumber();
                    serial_num_found = !serialNumberExists(serial_num);
                }
                torpedo_serial_nums[i].Push(serial_num);
                used_serial_nums.Add(serial_num);
            }

            itemInventoryUpdateRPC(1, i, torpedo_quantities[i]);
        }
    }

    //updates the entire inventory screen based on item_quantities and torpedo_quantities
    private void displayAdjustment()
    {
        List<int>[] current_quantities = new List<int>[] { item_quantities, torpedo_quantities };
        GameObject[] item_indicators = new GameObject[] { item_count_indicators, torpedo_count_indicators };
        for (int i = 0; i < 2; i++)
        {
            for (int c = 0; c < current_quantities[i].Count; c++)
            {
                //determine whether to fade or not based on there being at least one item
                float a = 1.0f;
                Color curr_color = item_indicators[i].transform.GetChild(c).GetComponent<TMP_Text>().color;
                if (current_quantities[i][c] <= 0)
                {
                    a = 0.2f;
                }

                //set the text
                item_indicators[i].transform.GetChild(c).GetComponent<TMP_Text>().color = new Color(curr_color.r, curr_color.g, curr_color.b, a);
                item_indicators[i].transform.GetChild(c).GetComponent<TMP_Text>().SetText(current_quantities[i][c].ToString());

                //set the icon
                item_indicators[i].transform.GetChild(c).GetChild(0).GetComponent<UnityEngine.UI.Image>().color = new Color(curr_color.r, curr_color.g, curr_color.b, a);
            }
        }
    }

    //helper method
    private Texture getTextureFromCategoryAndIndex(int item_category, int item_index)
    {
        GameObject[] item_indicators = new GameObject[] { item_count_indicators, torpedo_count_indicators };
        return item_indicators[item_category].transform.GetChild(item_index).GetChild(0).GetComponent<UnityEngine.UI.Image>().mainTexture;
    }

    //helper method
    private Color getColorFromCategoryAndIndex(int item_category, int item_index)
    {
        GameObject[] item_indicators = new GameObject[] { item_count_indicators, torpedo_count_indicators };
        return item_indicators[item_category].transform.GetChild(item_index).GetChild(0).GetComponent<UnityEngine.UI.Image>().color;
    }

    //returns the image of the item based on category (0 = normal items, 1 = torpedoes) and index
    public Texture getItemTexture(int item_category, int item_index)
    {
        GameObject[] item_indicators = new GameObject[] { item_count_indicators, torpedo_count_indicators };
        if (item_index >= 0 && (item_category >= 0 && item_category < 2))
        {
            if (item_index < item_indicators[item_category].transform.childCount)
            {
                return getTextureFromCategoryAndIndex(item_category, item_index);
            }
        }
        return null;
    }

    //helper method that returns 0 if normal item, 1 if torpedo item
    private int getItemCategoryFromName(string item_name)
    {
        if (ITEM_NAMES.Contains(item_name))
        {
            return 0;
        }
        else if (TORPEDO_NAMES.Contains(item_name))
        {
            return 1;
        }
        return -1;
    }

    //returns the index within the given list that item_name appears in (or -1 if not found)
    private int getItemIndexFromName(string item_name)
    {
        if (getItemCategoryFromName(item_name) < 0)
        {
            return -1;
        }
        List<string>[] possible_item_names = new List<string>[] { ITEM_NAMES, TORPEDO_NAMES };
        return possible_item_names[getItemCategoryFromName(item_name)].IndexOf(item_name);
    }

    //returns the image of the item based on string input
    public Texture getItemTexture(string item_name)
    {
        int item_category = getItemCategoryFromName(item_name);
        int item_index = getItemIndexFromName(item_name);

        if (item_index >= 0)
        {
            return getTextureFromCategoryAndIndex(item_category, item_index);
        }
        return null;
    }

    //returns the name of the item/torpedo type
    public string getItemName(int item_category, int item_index)
    {
        List<string>[] possible_item_names = new List<string>[] { ITEM_NAMES, TORPEDO_NAMES };
        if (item_index >= 0 && (item_category >= 0 && item_category < 2))
        {
            if (item_index < possible_item_names[item_category].Count)
            {
                return possible_item_names[item_category][item_index];
            }
        }
        return "";
    }

    //returns the color of the item/torpedo type
    public Color getItemColor(int item_category, int item_index)
    {
        if (getItemTexture(item_category, item_index) == null)
        {
            return Color.black;
        }

        return getColorFromCategoryAndIndex(item_category, item_index);
    }

    //returns the color of the item/torpedo type
    public Color getItemColor(string item_name)
    {
        if (getItemTexture(item_name) == null)
        {
            return Color.black;
        }

        int item_category = getItemCategoryFromName(item_name);
        int item_index = getItemIndexFromName(item_name);
        return getColorFromCategoryAndIndex(item_category, item_index);
    }

    //returns the number of item variations in a given category
    public int getNumberOfItemVariations(int item_category)
    {
        if (item_category < 0 || item_category > 1)
        {
            return -1;
        }

        List<string>[] possible_items = new List<string>[] { ITEM_NAMES, TORPEDO_NAMES };
        return possible_items[item_category].Count;
    }

    //links to ProbeController and CargoEjectLoader
    private void sendInventoryUpdates()
    {
        probe_controller.onInventoryChange(item_quantities[0]);
        cargo_eject_loader.onInventoryChange();
        shield_strength.onInventoryChange(item_quantities[2]);
        torpedo_loader.onInventoryChange();
    }

    private void addItemHelper(int item_category, int item_index, string serial_num)
    {
        int quantity = -1;

        if (item_category == 0)
        {
            item_quantities[item_index] += 1;
            quantity = item_quantities[item_index];
            item_serial_nums[item_index].Push(serial_num);
        }
        else
        {
            torpedo_quantities[item_index] += 1;
            quantity = torpedo_quantities[item_index];
            torpedo_serial_nums[item_index].Push(serial_num);
        }

        itemInventoryUpdateRPC(item_category, item_index, quantity);
    }

    private void addItemsHelper(int item_category, int item_index, Stack<string> serial_nums)
    {
        if (serial_nums.Count == 0)
        {
            return;
        }

        int quantity = -1;

        while (serial_nums.Count > 0)
        {
            string serial_num = serial_nums.Pop();
            if (item_category == 0)
            {
                item_quantities[item_index] += 1;
                quantity = item_quantities[item_index];
                item_serial_nums[item_index].Push(serial_num);
            }
            else
            {
                torpedo_quantities[item_index] += 1;
                quantity = torpedo_quantities[item_index];
                torpedo_serial_nums[item_index].Push(serial_num);
            }
        }

        itemInventoryUpdateRPC(item_category, item_index, quantity);
    }

    //adds as many items in the stack of serial numbers
    public void addItems(int item_category, int item_index, Stack<string> serial_nums)
    {
        if (getItemTexture(item_category, item_index) == null || NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        addItemsHelper(item_category, item_index, serial_nums);
    }

    //adds as many items as in the stack of serial numbers
    public void addItems(string item_name, Stack<string> serial_nums)
    {
        if (getItemTexture(item_name) == null || NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        int item_category = getItemCategoryFromName(item_name);
        int item_index = getItemIndexFromName(item_name);

        addItemsHelper(item_category, item_index, serial_nums);
    }

    //adds the item (if the name is valid)
    public void addItem(string item_name, string serial_num)
    {
        if (getItemTexture(item_name) == null || NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        int item_category = getItemCategoryFromName(item_name);
        int item_index = getItemIndexFromName(item_name);

        addItemHelper(item_category, item_index, serial_num);
    }

    //adds the item (if the category/index is valid)
    public void addItem(int item_category, int item_index, string serial_num)
    {
        if (getItemTexture(item_category, item_index) == null || NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        addItemHelper(item_category, item_index, serial_num);
    }

    //helper method for removeItem()
    private string removeItemHelper(int item_category, int item_index)
    {
        int quantity = -1;
        string serial_num = "";

        if (item_category == 0)
        {
            item_quantities[item_index] = Mathf.Max(0, item_quantities[item_index] - 1);
            quantity = item_quantities[item_index];
            serial_num = item_serial_nums[item_index].Pop();
        }
        else
        {
            torpedo_quantities[item_index] = Mathf.Max(0, torpedo_quantities[item_index] - 1);
            quantity = torpedo_quantities[item_index];
            serial_num = torpedo_serial_nums[item_index].Pop();
        }

        itemInventoryUpdateRPC(item_category, item_index, quantity);

        return serial_num;
    }

    //removes an item if it exists (or stays at 0 if already 0) and returns corresponding serial number
    public string removeItem(string item_name)
    {
        if (getItemTexture(item_name) == null || NetworkManager.Singleton.IsHost == false)
        {
            return "";
        }

        int item_category = getItemCategoryFromName(item_name);
        int item_index = getItemIndexFromName(item_name);

        return removeItemHelper(item_category, item_index);
    }

    //removes the item (if the category/index is valid) and returns corresponding serial number
    public string removeItem(int item_category, int item_index)
    {
        if (getItemTexture(item_category, item_index) == null || NetworkManager.Singleton.IsHost == false)
        {
            return "";
        }

        return removeItemHelper(item_category, item_index);
    }

    //sets the quantity of an item
    public void setItemQuantity(int item_category, int item_index, int new_quantity)
    {
        if (getItemTexture(item_category, item_index) == null || NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        if (item_category == 0)
        {
            item_quantities[item_index] = new_quantity;
        }
        else
        {
            torpedo_quantities[item_index] = new_quantity;
        }

        itemInventoryUpdateRPC(item_category, item_index, new_quantity);
    }

    //returns the quantity of that item (or -1 if incorrect name)
    public int getItemQuantity(string item_name)
    {
        if (getItemTexture(item_name) == null)
        {
            return -1;
        }

        int item_category = getItemCategoryFromName(item_name);
        int item_index = getItemIndexFromName(item_name);

        if (item_category == 0)
        {
            return item_quantities[item_index];
        }
        return torpedo_quantities[item_index];
    }

    //returns the quantity of that item (or -1 if category/index is invalid)
    public int getItemQuantity(int item_category, int item_index)
    {
        if (getItemTexture(item_category, item_index) == null)
        {
            return -1;
        }

        if (item_category == 0)
        {
            return item_quantities[item_index];
        }
        return torpedo_quantities[item_index];
    }

    //returns the item's ID
    public int getItemID(string item_name)
    {
        if (getItemTexture(item_name) == null)
        {
            return -1;
        }

        int item_category = getItemCategoryFromName(item_name);
        int item_index = getItemIndexFromName(item_name);

        if (item_category == 0)
        {
            return ITEM_IDS[item_index];
        }
        return TORPEDO_IDS[item_index];
    }

    //returns the item's weight
    public int getItemWeight(string item_name)
    {
        if (getItemTexture(item_name) == null)
        {
            return -1;
        }

        int item_category = getItemCategoryFromName(item_name);
        int item_index = getItemIndexFromName(item_name);

        if (item_category == 0)
        {
            return ITEM_WEIGHTS[item_index];
        }
        return TORPEDO_WEIGHTS[item_index];
    }

    //returns a Vector2 of the item's height and length
    public Vector2 getItemSize(string item_name)
    {
        if (getItemTexture(item_name) == null)
        {
            return Vector2.zero;
        }

        int item_category = getItemCategoryFromName(item_name);
        int item_index = getItemIndexFromName(item_name);

        if (item_category == 0)
        {
            return ITEM_SIZES[item_index];
        }
        return TORPEDO_SIZES[item_index];
    }

    //shows inventory screen
    public void powerOn(int position)
    {
        inventory_display.SetActive(true);
    }

    //hides inventory screen
    public void powerOff(int position, float time)
    {
        inventory_display.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void itemInventoryUpdateRPC(int item_category, int item_index, int quantity)
    {
        if (item_category == 0)
        {
            item_quantities[item_index] = quantity;
        }
        else
        {
            torpedo_quantities[item_index] = quantity;
        }
        displayAdjustment();
        sendInventoryUpdates();
    }
}