/*
    TacticianMap.cs
    - Handles tactician radar map
    Contributor(s): Jake Schott
    Last Updated: 7/25/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticianMap : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float MAP_UPDATE_DELAY = 1.5f; //updates every 1.5 seconds

    private GameObject this_ship;
    private GameObject world_root;
    public GameObject map_display;
    private GameObject natural_phenomena;
    private GameObject ships;
    private MapOptions map_options; //used for zooming

    private float[] corresponding_sizes = new float[0];
    private GameObject[] corresponding_icons = new GameObject[0];
    private Color[] corresponding_colors = new Color[0]; 
    private Vector3[] corresponding_locations = new Vector3[0];
    private Coroutine item_flasher_coroutine = null;

    void Start()
    {
        map_options = GameObject.FindGameObjectWithTag("ControlHandler").GetComponent<MapOptions>();
        this_ship = GameObject.FindGameObjectWithTag("Spaceship");
        world_root = GameObject.FindGameObjectWithTag("WorldRoot");
        natural_phenomena = map_display.transform.GetChild(3).gameObject;
        ships = map_display.transform.GetChild(4).gameObject;

        StartCoroutine(mapUpdater());    
    }

    IEnumerator itemFlasher()
    {
        float anim_time = MAP_UPDATE_DELAY;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time -= dt;
            for (int i = 0; i < corresponding_icons.Length; i++)
            {
                corresponding_icons[i].GetComponent<UnityEngine.UI.RawImage>().color =
                    new Color(corresponding_colors[i].r,
                              corresponding_colors[i].g,
                              corresponding_colors[i].b,
                              Mathf.Lerp(0.0f, 0.5f, anim_time / MAP_UPDATE_DELAY));
            }
            yield return null;
        }
    }

    //clears all items from the map
    private void resetMap()
    {
        GameObject[] to_reset = new GameObject[2] { natural_phenomena, ships };
        for (int i = 0; i < 2; i++)
        {
            for (int m = to_reset[i].transform.childCount - 1; m >= 1; m--)
            {
                Object.Destroy(to_reset[i].transform.GetChild(m).gameObject);
            }
        }
    }

    public void zoomMap()
    {
        float zoom_percentage = map_options.getZoom(); //1.0 is full zoom; 0.0 is fully-zoomed out

        for (int i = 0; i < corresponding_icons.Length; i++)
        {
            float x_coordinate = corresponding_locations[i].x * (0.00025f + ((zoom_percentage) * 0.00025f));
            float z_coordinate = corresponding_locations[i].z * (0.00025f + ((zoom_percentage) * 0.00025f));
            corresponding_icons[i].transform.localPosition =
                new Vector3(-x_coordinate,
                            z_coordinate,
                            0.0f);

            float item_size = Mathf.Max(0.005f, corresponding_sizes[i] * (0.001f + (zoom_percentage * 0.001f)));
            corresponding_icons[i].GetComponent<RectTransform>().sizeDelta = new Vector2(item_size, item_size);
            corresponding_icons[i].SetActive(Mathf.Abs(corresponding_icons[i].transform.localPosition.x) < 0.23f && Mathf.Abs(corresponding_icons[i].transform.localPosition.y) < 0.23f);
        }

    }

    public void rotateMap()
    {
        natural_phenomena.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -this_ship.transform.localEulerAngles.y);
        ships.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -this_ship.transform.localEulerAngles.y);
    }

    private void updateMap()
    {
        List<GameObject> map_items = new List<GameObject>();

        if(world_root == null)
        {
            world_root = GameObject.FindGameObjectWithTag("WorldRoot");
        }


        foreach (Transform m in world_root.transform)
        {
            Component[] item_components = m.GetComponents<Component>();
            for (int i = 0; i < item_components.Length; i++)
            {
                MapItem test_map_item = item_components[i] as MapItem;
                if (test_map_item != null)
                {
                    if (test_map_item.isVisible() && Vector3.Distance(this_ship.transform.position, m.position) < 1000.0f)
                    {
                        map_items.Add(m.gameObject);
                    }
                }
            }
        }
        corresponding_locations = new Vector3[map_items.Count];
        corresponding_icons = new GameObject[map_items.Count];
        corresponding_colors = new Color[map_items.Count];
        corresponding_sizes = new float[map_items.Count];

        for (int i = 0; i < map_items.Count; i++)
        {
            corresponding_locations[i] =
                new Vector3(map_items[i].transform.position.x,
                            this_ship.transform.position.y,
                            map_items[i].transform.position.z);

            GameObject item_to_add = null;
            MapItem item_info = map_items[i].GetComponent<MapItem>();
            bool item_is_ship = item_info.isShip();
            if (item_is_ship == true)
            {
                item_to_add = GameObject.Instantiate(ships.transform.GetChild(0).gameObject, ships.transform);
            }
            else
            {
                item_to_add = GameObject.Instantiate(natural_phenomena.transform.GetChild(0).gameObject, natural_phenomena.transform);
            }

            corresponding_locations[i] -= this_ship.transform.position;

            item_to_add.transform.localPosition =
                new Vector3(corresponding_locations[i].x * 0.001f,
                            corresponding_locations[i].z * 0.001f,
                            0.0f);

            item_to_add.GetComponent<UnityEngine.UI.RawImage>().texture = item_info.getIcon();
            Color icon_color = item_info.getColor();
            item_to_add.GetComponent<UnityEngine.UI.RawImage>().color = new Color(icon_color.r, icon_color.g, icon_color.b, 0.5f);

            corresponding_icons[i] = item_to_add;
            corresponding_colors[i] = icon_color;
            corresponding_sizes[i] = item_info.getSize();
        }

        //adjust to current zoom configuration
        zoomMap();

        //flash all items
        if (item_flasher_coroutine != null)
        {
            StopCoroutine(item_flasher_coroutine);
        }
        StartCoroutine(itemFlasher());
    }

    IEnumerator mapUpdater()
    {
        while (true)
        {
            resetMap();
            updateMap();
            yield return new WaitForSeconds(MAP_UPDATE_DELAY);
        }
    }
}
