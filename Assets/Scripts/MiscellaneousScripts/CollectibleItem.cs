/*
    CollectibleItem.cs
    - Implements ITractorBeamable
    - Used for items that can be collected once pulled in by the tractor beam
    - Handles illuminating/hiding objects (activated/deactivated by ShipBeacon in captain position)
    Contributor(s): Jake Schott
    Last Updated: 3/22/2026
*/ 

using Unity.Netcode;
using UnityEngine;

public class CollectibleItem : MonoBehaviour, ITractorBeamable, IDamageable
{
    //CLASS CONSTANTS
    private static float BRIGHTNESS_FACTOR = 5.0f; //5 times brighter when ship beacon enabled

    public Material pure_black;
    public GameObject lit_mesh;
    public GameObject lights;

    [SerializeField]
    private int item_category; //0 = normal items, 1 = torpedoes
    [SerializeField]
    private int item_index; //index according to ShipInventory
    [SerializeField]
    private bool is_illuminated = false; //whether it is illuminated in space or not
    [SerializeField]
    private float item_health = 5.0f;
    [SerializeField]
    private Color explosion_color;
    [SerializeField]
    private float explosion_size;

    private Material starting_material;
    private float[] starting_light_intensities;
    private string serial_num; //unique identifier
    private bool is_probe = false;

    private void Start()
    {
        //if not host, destroy collider/rigidbody and rely on Network Object to send transform updates through host
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(transform.GetComponent<Collider>());
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

    public void damage(float damage, IDamageable.DamageType damage_type)
    {
        if (NetworkManager.Singleton.IsHost == false || is_probe == true || item_health <= 0.0f) //probe damage handled by Probe.cs
        {
            return;
        }

        item_health = Mathf.Max(0.0f, item_health - damage);

        //handle destruction
        if (item_health <= 0.0f)
        {
            ReferenceAssistor.Instance.effects_handler.createExplosion(transform.position, explosion_size, false, explosion_color);
            GetComponent<NetworkObject>().Despawn(true);
        }
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

    public bool getTractorBeamable()
    {
        return true;
    }

    public Texture getItemTexture()
    {
        return ReferenceAssistor.Instance.spaceship.GetComponent<ShipInventory>().getItemTexture(item_category, item_index);
    }

    public Color getItemColor()
    {
        return ReferenceAssistor.Instance.spaceship.GetComponent<ShipInventory>().getItemColor(item_category, item_index);
    }

    public int getItemCategory()
    {
        return item_category;
    }

    public int getItemIndex()
    {
        return item_index;
    }

    public string getSerialNumber()
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
            if (index >= ShipInventory.ITEM_NAMES.Count)
            {
                return;
            }
        }
        else
        {
            if (index >= ShipInventory.TORPEDO_NAMES.Count)
            {
                return;
            }
        }

        item_index = index;
    }

    public void setSerialNumber(string serial)
    {
        serial_num = serial;
    }
}