/*
    ProximityMap.cs
    - Handles tactician radar map
    Contributor(s): Jake Schott
    Last Updated: 8/11/2026
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
    private static float MAP_CENTER_SIZE = 130.0f; //the triangle
    private static float POS_CONVERSION_FACTOR = (MAP_CUTOFF) / (ScenarioManager.BOUNDARY_SIZE * MAP_SIZE_RELATIVE_TO_BOUNDARY * 0.5f);

    public GameObject proximity_map_display;
    public GameObject proximity_map_renderer;
    public Sprite proximity_map_circular_icon;

    private GameObject this_ship;
    private GameObject world_root;
    private GameObject map_center_icon;
    private ProximityMapOptions proximity_map_options; //used for zooming

    private float[] corresponding_sizes = new float[0];
    private GameObject[] corresponding_icons = new GameObject[0];
    private Color[] corresponding_colors = new Color[0];
    private Vector2[] corresponding_locations = new Vector2[0];
    private Vector2[][] phaser_locations = new Vector2[][] 
    { 
        new Vector2[]{ new Vector2(), new Vector2() },
        new Vector2[]{ new Vector2(), new Vector2() },
        new Vector2[]{ new Vector2(), new Vector2() },
    };
    private Coroutine map_updater_coroutine = null;
    private Coroutine item_flasher_coroutine = null;

    private void Start()
    {
        this_ship = ReferenceAssistor.Instance.spaceship;
        world_root = ReferenceAssistor.Instance.world_root;
        map_center_icon = proximity_map_display.transform.GetChild(1).GetChild(0).gameObject;
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
                Color c = corresponding_colors[i];
                c.a = a;
                corresponding_icons[i].GetComponent<SpriteRenderer>().color = c;
            }

            yield return null;
        }
    }

    //deallocates map icons for future use
    private void resetMap()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int m = proximity_map_renderer.transform.GetChild(0).GetChild(i + 1).childCount - 1; m >= 0; m--)
            {
                GameObject map_item_to_reallocate = proximity_map_renderer.transform.GetChild(0).GetChild(i + 1).GetChild(m).gameObject;
                map_item_to_reallocate.transform.parent = proximity_map_renderer.transform.GetChild(0).GetChild(0);
                map_item_to_reallocate.GetComponent<SpriteRenderer>().sprite = proximity_map_circular_icon;
                map_item_to_reallocate.SetActive(false);
            }
        }
    }

    //returns Vector3 coordinate from world positions x and z
    public Vector3 getProximityMapLocation(float zoom_percentage, Vector2 coordinate)
    {
        float x_coordinate = coordinate.x * POS_CONVERSION_FACTOR;
        x_coordinate = x_coordinate + (zoom_percentage * x_coordinate);
        float z_coordinate = coordinate.y * POS_CONVERSION_FACTOR;
        z_coordinate = z_coordinate + (zoom_percentage * z_coordinate);
        return new Vector3(x_coordinate, z_coordinate, 0.0f);
    }

    public void zoomMap()
    {
        float zoom_percentage = proximity_map_options.getZoom(); //1.0 is full zoom; 0.0 is fully-zoomed out

        //adjust background rings
        for (int i = 0; i < 7; i++)
        {
            float circle_radius = 0.0475f + (0.0325f * (6.0f - i));
            float circle_diameter = circle_radius + (zoom_percentage * circle_radius);
            proximity_map_display.transform.GetChild(0).GetChild(i).gameObject.SetActive(!(circle_diameter > (MAP_CUTOFF * 2.0f)));

            //adjust ring
            proximity_map_display.transform.GetChild(0).GetChild(i).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(circle_diameter, circle_diameter);

            //adjust coverup
            proximity_map_display.transform.GetChild(0).GetChild(i).GetChild(0).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(circle_diameter - 0.0025f, circle_diameter - 0.0025f);
        }

        //adjust ship triangle in center of map
        float center_size = (MAP_CENTER_SIZE / (ScenarioManager.BOUNDARY_SIZE * MAP_SIZE_RELATIVE_TO_BOUNDARY)) * (MAP_CUTOFF * 2.0f);
        center_size = center_size + (center_size * zoom_percentage);
        map_center_icon.GetComponent<RectTransform>().sizeDelta = new Vector2(center_size, center_size);
        map_center_icon.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(center_size, center_size);

        //adjust map items
        for (int i = 0; i < corresponding_icons.Length; i++)
        {
            corresponding_icons[i].transform.localPosition = getProximityMapLocation(zoom_percentage, corresponding_locations[i]);

            //handle map item resizing
            float item_size = (corresponding_sizes[i] / (ScenarioManager.BOUNDARY_SIZE * MAP_SIZE_RELATIVE_TO_BOUNDARY)) * (MAP_CUTOFF * 2.0f) * (0.1f);
            item_size = item_size + (item_size * zoom_percentage);
            corresponding_icons[i].transform.localScale = new Vector3(item_size, item_size, 1.0f);
            corresponding_icons[i].SetActive(Mathf.Abs(corresponding_icons[i].transform.localPosition.x) < (MAP_CUTOFF + (item_size * 5.0f)) && Mathf.Abs(corresponding_icons[i].transform.localPosition.y) < (MAP_CUTOFF + (item_size * 5.0f)));
        }

        //adjust phasers
        for (int i = 0; i < 3; i++)
        {
            if (proximity_map_renderer.transform.GetChild(0).GetChild(4).gameObject.activeSelf == true)
            {
                displayPhaser(i);
            }
        }
    }

    private void displayPhaser(int index)
    {
        float zoom_percentage = proximity_map_options.getZoom();

        //set size of phaser
        float width = Mathf.Lerp(0.0004f, 0.0008f, zoom_percentage);
        float length = (Vector2.Distance(phaser_locations[index][0], phaser_locations[index][1])) * POS_CONVERSION_FACTOR;
        length = (length + (length * zoom_percentage)) * 0.68f;
        proximity_map_renderer.transform.GetChild(0).GetChild(4).GetChild(index).localScale = new Vector3(width, length, 1.0f);

        //set position of phaser
        proximity_map_renderer.transform.GetChild(0).GetChild(4).GetChild(index).localPosition = getProximityMapLocation(zoom_percentage, (phaser_locations[index][0] + phaser_locations[index][1]) / 2.0f);

        //set rotation of phaser
        Vector2 dir = getProximityMapLocation(zoom_percentage, phaser_locations[index][0]) - getProximityMapLocation(zoom_percentage, phaser_locations[index][1]);
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        proximity_map_renderer.transform.GetChild(0).GetChild(4).GetChild(index).localRotation = Quaternion.Euler(0.0f, 0.0f, -angle);
    }

    public void rotateMap(float ship_rotation)
    {
        proximity_map_renderer.transform.GetChild(0).transform.localRotation = Quaternion.Euler(0.0f, 0.0f, ship_rotation);
    }

    private void updateMap()
    {
        List<GameObject> map_items = new List<GameObject>();

        world_root = ReferenceAssistor.Instance.world_root;
        if (world_root == null)
        {
            return;
        }

        foreach (Transform m in world_root.transform)
        {
            MapItem map_item = m.GetComponent<MapItem>();
            if (map_items.Count < 100 && map_item != null)
            {
                Vector2 m_position_xy = new Vector2(m.position.x, m.position.z);
                if (map_item.isVisible() && Vector2.Distance(Vector2.zero, m_position_xy) < ((map_item.getSize() * 0.5f) + ScenarioManager.BOUNDARY_SIZE * MAP_SIZE_RELATIVE_TO_BOUNDARY * 0.5f))
                {
                    map_items.Add(m.gameObject);
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
            item_to_add = proximity_map_renderer.transform.GetChild(0).GetChild(0).GetChild(0).gameObject;
            item_to_add.transform.parent = proximity_map_renderer.transform.GetChild(0).GetChild(type_index + 1);

            //if ship or obstacle, rotate
            if (type_index == 0 || type_index == 2)
            {
                item_to_add.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -item_info.transform.eulerAngles.y);
            }
            else
            {
                item_to_add.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -proximity_map_renderer.transform.GetChild(0).transform.localRotation.eulerAngles.z);
            }

            Color icon_color = item_info.getColor();
            Sprite icon_texture = item_info.getSprite();
            item_to_add.GetComponent<SpriteRenderer>().color = new Color(icon_color.r, icon_color.g, icon_color.b, 0.5f);
            if (icon_texture != null)
            {
                item_to_add.GetComponent<SpriteRenderer>().sprite = icon_texture;
            }

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

    public void showPhaser(int index, Vector3 start_pos, Vector3 end_pos)
    {
        proximity_map_renderer.transform.GetChild(0).GetChild(4).GetChild(index).gameObject.SetActive(true);
        phaser_locations[index][0] = new Vector2(start_pos.x, start_pos.z);
        phaser_locations[index][1] = new Vector2(end_pos.x, end_pos.z);
        displayPhaser(index);
    }

    public void hidePhaser(int index)
    {
        proximity_map_renderer.transform.GetChild(0).GetChild(4).GetChild(index).gameObject.SetActive(false);
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
        proximity_map_display.SetActive(true);
        proximity_map_renderer.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        if (map_updater_coroutine != null)
        {
            StopCoroutine(map_updater_coroutine);
            map_updater_coroutine = null;
        }
        proximity_map_display.SetActive(false);
        proximity_map_renderer.SetActive(false);
    }
}