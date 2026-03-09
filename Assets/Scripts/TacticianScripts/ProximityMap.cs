/*
    ProximityMap.cs
    - Handles tactician radar map
    Contributor(s): Jake Schott
    Last Updated: 3/7/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProximityMap : MonoBehaviour, IPowerable
{
    //CLASS CONSTANTS
    private static float MAP_UPDATE_DELAY = 1.5f; //updates every 1.5 seconds
    private static float MAP_CUTOFF = 0.138f;
    private static float MAP_SIZE_RELATIVE_TO_BOUNDARY = 0.5f; //50% the size of the boundary
    private static float MAP_CENTER_SIZE = 100.0f; //the triangle

    public GameObject map_display;

    private GameObject this_ship;
    private GameObject world_root;
    private GameObject map_center_icon;
    private ProximityMapOptions proximity_map_options; //used for zooming

    private float[] corresponding_sizes = new float[0];
    private GameObject[] corresponding_icons = new GameObject[0];
    private Color[] corresponding_colors = new Color[0];
    private Vector2[] corresponding_locations = new Vector2[0];
    private Coroutine map_updater_coroutine = null;
    private Coroutine item_flasher_coroutine = null;

    void Start()
    {
        this_ship = GameObject.FindGameObjectWithTag("Spaceship");
        world_root = GameObject.FindGameObjectWithTag("WorldRoot");
        map_center_icon = map_display.transform.GetChild(1).GetChild(0).gameObject;
        proximity_map_options = GetComponent<ProximityMapOptions>();
    }

    IEnumerator itemFlasher()
    {
        float anim_time = MAP_UPDATE_DELAY;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            float a = Mathf.Lerp(0.0f, 0.5f, anim_time / MAP_UPDATE_DELAY);
            for (int i = 0; i < corresponding_icons.Length; i++)
            {
                corresponding_icons[i].GetComponent<UnityEngine.UI.RawImage>().color =
                    new Color(corresponding_colors[i].r, corresponding_colors[i].g, corresponding_colors[i].b, a);
            }

            yield return null;
        }
    }

    //clears all items from the map
    private void resetMap()
    {
        for (int m = map_display.transform.GetChild(2).childCount - 1; m >= 1; m--)
        {
            Object.Destroy(map_display.transform.GetChild(2).transform.GetChild(m).gameObject);
        }
    }

    public void zoomMap()
    {
        float zoom_percentage = proximity_map_options.getZoom(); //1.0 is full zoom; 0.0 is fully-zoomed out

        //adjust background rings
        for (int i = 0; i < 7; i++)
        {
            float circle_radius = 0.0475f + (0.0325f * (6.0f - i));
            float circle_diameter = circle_radius + (zoom_percentage * circle_radius);
            map_display.transform.GetChild(0).GetChild(i).gameObject.SetActive(!(circle_diameter > (MAP_CUTOFF * 2.0f)));

            //adjust ring
            map_display.transform.GetChild(0).GetChild(i).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(circle_diameter, circle_diameter);

            //adjust coverup
            map_display.transform.GetChild(0).GetChild(i).GetChild(0).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(circle_diameter - 0.0025f, circle_diameter - 0.0025f);
        }

        //adjust ship triangle in center of map
        float center_size = (MAP_CENTER_SIZE / (ScenarioManager.BOUNDARY_SIZE * MAP_SIZE_RELATIVE_TO_BOUNDARY)) * (MAP_CUTOFF * 2.0f);
        center_size = center_size + (center_size * zoom_percentage);
        map_center_icon.GetComponent<RectTransform>().sizeDelta = new Vector2(center_size, center_size);

        float pos_conversion_factor = (MAP_CUTOFF) / (ScenarioManager.BOUNDARY_SIZE * MAP_SIZE_RELATIVE_TO_BOUNDARY * 0.5f);

        //adjust map items
        for (int i = 0; i < corresponding_icons.Length; i++)
        {
            //handle map item positioning
            float x_coordinate = corresponding_locations[i].x * pos_conversion_factor;
            x_coordinate = x_coordinate + (zoom_percentage * x_coordinate);
            float z_coordinate = corresponding_locations[i].y * pos_conversion_factor;
            z_coordinate = z_coordinate + (zoom_percentage * z_coordinate);
            corresponding_icons[i].transform.localPosition =
                new Vector3(x_coordinate, z_coordinate, 0.0f);

            //handle map item resizing
            float item_size = (corresponding_sizes[i] / (ScenarioManager.BOUNDARY_SIZE * MAP_SIZE_RELATIVE_TO_BOUNDARY)) * (MAP_CUTOFF * 2.0f);
            item_size = item_size + (item_size * zoom_percentage);
            corresponding_icons[i].GetComponent<RectTransform>().sizeDelta = new Vector2(item_size, item_size);
            corresponding_icons[i].SetActive(Mathf.Abs(corresponding_icons[i].transform.localPosition.x) < (MAP_CUTOFF + 0.02f) && Mathf.Abs(corresponding_icons[i].transform.localPosition.y) < (MAP_CUTOFF + 0.02f));
        }
    }

    public void rotateMap()
    {
        map_display.transform.GetChild(2).transform.localRotation = Quaternion.Euler(0.0f, 0.0f, this_ship.transform.localEulerAngles.y);
    }

    private void updateMap()
    {
        List<GameObject> map_items = new List<GameObject>();

        world_root = GameObject.FindGameObjectWithTag("WorldRoot");
        if (world_root == null)
        {
            return;
        }

        foreach (Transform m in world_root.transform)
        {
            Component[] item_components = m.GetComponents<Component>();
            for (int i = 0; i < item_components.Length; i++)
            {
                MapItem test_map_item = item_components[i] as MapItem;
                if (test_map_item != null)
                {
                    Vector2 m_position_xy = new Vector2(m.position.x, m.position.z);
                    if (test_map_item.isVisible() && Vector2.Distance(Vector2.zero, m_position_xy) < (ScenarioManager.BOUNDARY_SIZE * MAP_SIZE_RELATIVE_TO_BOUNDARY * 0.5f))
                    {
                        map_items.Add(m.gameObject);
                    }
                }
            }
        }

        corresponding_locations = new Vector2[map_items.Count];
        corresponding_icons = new GameObject[map_items.Count];
        corresponding_colors = new Color[map_items.Count];
        corresponding_sizes = new float[map_items.Count];

        for (int i = 0; i < map_items.Count; i++)
        {
            corresponding_locations[i] =
                new Vector2(map_items[i].transform.position.x, map_items[i].transform.position.z);

            GameObject item_to_add = null;
            MapItem item_info = map_items[i].GetComponent<MapItem>();
            bool item_is_ship = item_info.isShip();
            int type_index = 0; //obstacle
            if (item_is_ship == true)
            {
                type_index = 2; //ship
            }
            else if (item_info.gameObject.GetComponent<CollectibleItem>() != null)
            {
                type_index = 1; //collectible item
            }
            item_to_add = GameObject.Instantiate(map_display.transform.GetChild(2).GetChild(0).gameObject, map_display.transform.GetChild(2));

            //if ship or obstacle, rotate
            if (type_index == 0 || type_index == 2)
            {
                item_to_add.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -item_info.transform.eulerAngles.y);
            }
            else
            {
                item_to_add.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -map_display.transform.GetChild(3).transform.localRotation.eulerAngles.z);
            }

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

    public void powerOn(int position)
    {
        if (map_updater_coroutine == null)
        {
            map_updater_coroutine = StartCoroutine(mapUpdater());
        }
        map_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        if (map_updater_coroutine != null)
        {
            StopCoroutine(map_updater_coroutine);
            map_updater_coroutine = null;
        }
        map_display.SetActive(false);
    }
}