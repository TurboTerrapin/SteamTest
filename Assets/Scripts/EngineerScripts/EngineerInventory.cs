/*
    EngineerInventory.cs
    - Currently only enables/disables inventory screen
    Contributor(s): Jake Schott
    Last Updated: 10/17/2025
*/

using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class EngineerInventory : NetworkBehaviour, IPowerable
{
    public GameObject inventory_display;

    private GameObject item_count_indicators;
    private GameObject torpedo_count_indicators;

    private List<int> item_quantities = new List<int>() { 1, 4, 5 };
    private List<int> item_ids = new List<int>() { 102824, 104308, 102110 };
    private List<int> item_weights = new List<int>() { 1200, 3500, 500 };
    private List<Vector2> item_sizes = new List<Vector2>() { new Vector2(3.6f, 3.6f), new Vector2(4.2f, 4.8f), new Vector2(3.3f, 4.1f) };
    private List<string> item_names = new List<string>() { "Probe", "Escape Pod", "Shield Battery" };

    private List<int> torpedo_quantities = new List<int>() { 10, 4, 2, 1, 0, 0 };
    private List<int> torpedo_ids = new List<int>() { 302025, 302022, 302001, 301995, 301997, 382000};
    private List<int> torpedo_weights = new List<int>() { 5000, 3350, 6000, 1100, 500, 8900 };
    private List<Vector2> torpedo_sizes = new List<Vector2>() { new Vector2(2.1f, 3.5f), new Vector2(2.1f, 3.5f), new Vector2(2.1f, 3.5f), new Vector2(1.5f, 3.5f), new Vector2(2.1f, 3.5f), new Vector2(2.5f, 4.1f) };
    private List<string> torpedo_names = new List<string>() { "Photon", "Ion", "Proton", "Quantum", "Superluminal", "Chroniton" };

    private void Start()
    {
        item_count_indicators = inventory_display.transform.GetChild(1).gameObject;
        torpedo_count_indicators = inventory_display.transform.GetChild(2).gameObject;

        displayAdjustment();
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
        if (item_names.Contains(item_name))
        {
            return 0;
        }
        else if (torpedo_names.Contains(item_name))
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
        List<string>[] possible_item_names = new List<string>[] { item_names, torpedo_names };
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
        List<string>[] possible_item_names = new List<string>[] { item_names, torpedo_names };
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

        List<string>[] possible_items = new List<string>[] { item_names, torpedo_names };
        return possible_items[item_category].Count; 
    }

    //adds the item (if the name is valid)
    public void addItem(string item_name)
    {
        if (getItemTexture(item_name) == null)
        {
            return;
        }

        int item_category = getItemCategoryFromName(item_name);
        int item_index = getItemIndexFromName(item_name);

        if (item_category == 0)
        {
            item_quantities[item_index] += 1;
        }
        else
        {
            torpedo_quantities[item_index] += 1;
        }
        displayAdjustment();
    }

    //adds the item (if the category/index is valid)
    public void addItem(int item_category, int item_index)
    {
        if (getItemTexture(item_category, item_index) == null)
        {
            return;
        }

        if (item_category == 0)
        {
            item_quantities[item_index] += 1;
        }
        else
        {
            torpedo_quantities[item_index] += 1;
        }
        displayAdjustment();
    }

    //removes an item if it exists (or stays at 0 if already 0)
    public void removeItem(string item_name)
    {
        if (getItemTexture(item_name) == null)
        {
            return;
        }

        int item_category = getItemCategoryFromName(item_name);
        int item_index = getItemIndexFromName(item_name);

        if (item_category == 0)
        {
            item_quantities[item_index] = Mathf.Max(0, item_quantities[item_index] - 1);
        }
        else
        {
            torpedo_quantities[item_index] = Mathf.Max(0, torpedo_quantities[item_index] - 1);
        }
        displayAdjustment();
    }

    //removes the item (if the category/index is valid)
    public void removeItem(int item_category, int item_index)
    {
        if (getItemTexture(item_category, item_index) == null)
        {
            return;
        }

        if (item_category == 0)
        {
            item_quantities[item_index] = Mathf.Max(0, item_quantities[item_index] - 1);
        }
        else
        {
            torpedo_quantities[item_index] = Mathf.Max(0, torpedo_quantities[item_index] - 1);
        }
        displayAdjustment();
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
            return item_ids[item_index];
        }
        return torpedo_ids[item_index];
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
            return item_weights[item_index];
        }
        return torpedo_weights[item_index];
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
            return item_sizes[item_index];
        }
        return torpedo_sizes[item_index];
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
}