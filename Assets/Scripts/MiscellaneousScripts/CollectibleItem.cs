/*
    CollectibleItem.cs
    - Used for items that can be collected once pulled in by the tractor beam
    - Handles illuminating/hiding objects (activated/deactivated by ShipBeacon in captain position)
    Contributor(s): Jake Schott
    Last Updated: 1/23/2026
*/

using Unity.Netcode;
using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float BRIGHTNESS_FACTOR = 5.0f; //5 times brighter when ship beacon enabled

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
    private float[] starting_light_intensities;
    private int serial_num; //unique identifier
    private bool is_probe = false;

    private void Start()
    {
        //if not host, destroy collider/rigidbody and rely on Network Object to send transform updates through host
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(transform.GetComponent<Collider>());
            Component.Destroy(transform.GetComponent<Rigidbody>());
        }

        if (lit_mesh != null)
        {
            starting_material = lit_mesh.GetComponent<Renderer>().material;
        }

        if (lights != null)
        {
            starting_light_intensities = new float[lights.transform.childCount];
            for (int i = 0; i < lights.transform.childCount; i++)
            {
                starting_light_intensities[i] = lights.transform.GetChild(i).GetComponent<Light>().intensity;
            }
        }

        setIlluminated(is_illuminated);
        is_probe = (transform.GetComponent<Probe>() != null);
    }

    public void setIlluminated(bool illuminated)
    {
        if (is_probe == true)
        {
            return;
        }

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

    public void setIlluminationIntensity(float intensity)
    {
        if (lights == null)
        {
            return;
        }

        for (int i = 0; i < lights.transform.childCount; i++)
        {
            lights.transform.GetChild(i).GetComponent<Light>().intensity = Mathf.Lerp(starting_light_intensities[i], starting_light_intensities[i] * BRIGHTNESS_FACTOR, intensity);
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
