/*
    CollectibleItem.cs
    - Used for items that can be collected once pulled in by the tractor beam
    - Handles illuminating/hiding objects (activated/deactivated by ShipBeacon in captain position)
    Contributor(s): Jake Schott
    Last Updated: 12/9/2025
*/

using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    public Material pure_black;
    public GameObject lit_mesh;
    public GameObject lights;

    [SerializeField]
    private int item_category; //0 = normal items, 1 = torpedoes
    [SerializeField]
    private int item_index; //index according to EngineerInventory
    [SerializeField]
    private bool is_illuminated = false; //whether it is illuminated in space or not

    private Material starting_material;
    private int serial_num; //unique identifier

    private void Start()
    {
        if (lit_mesh != null)
        {
            starting_material = lit_mesh.GetComponent<Renderer>().material;
        }

        setIlluminated(is_illuminated);
    }

    public void setIlluminated(bool illuminated)
    {
        is_illuminated = illuminated;

        if (lit_mesh != null)
        {
            if (illuminated == true)
            {
                lit_mesh.GetComponent<Renderer>().material = starting_material;
            }
            else
            {
                lit_mesh.GetComponent<Renderer>().material = pure_black;
            }
        }

        if (lights != null)
        {
            lights.SetActive(illuminated);
        }
    }

    public int getItemCategory()
    {
        return item_category;
    }

    public int getItemIndex()
    {
        return item_index;
    }

    public int getSerialNumber()
    {
        return serial_num;
    }

    public void setItemCategory(int category)
    {
        if (category < 0 || category > 1)
        {
            return;
        }
        item_category = category;   
    }

    public void setItemIndex(int index)
    {
        if (index < 0)
        {
            return;
        }

        if (item_category == 0)
        {
            if (index >= EngineerInventory.ITEM_NAMES.Count)
            {
                return;
            }
        }
        else
        {
            if (index >= EngineerInventory.TORPEDO_NAMES.Count)
            {
                return;
            }
        }

        item_index = index;
    }

    public void setSerialNumber(int serial)
    {
        if (serial < 0 || serial > 99999)
        {
            return;
        }

        serial_num = serial;
    }
}
