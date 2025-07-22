/*
    TacticianMap.cs
    - Handles tactician radar map
    Contributor(s): Jake Schott
    Last Updated: 7/20/2025
*/

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TacticianMap : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float MAP_UPDATE_DELAY = 1.0f; //updates every second

    private GameObject this_ship;
    public GameObject map_display;
    private GameObject natural_phenomena;
    private GameObject ships;
    private MapOptions map_options; //used for zooming

    private GameObject[] map_items = null;
    private GameObject[] corresponding_icons = null;
    private Vector3[] corresponding_locations = null;

    void Start()
    {
        map_options = GameObject.FindGameObjectWithTag("ControlHandler").GetComponent<MapOptions>();
        this_ship = GameObject.FindGameObjectWithTag("Spaceship");
        natural_phenomena = map_display.transform.GetChild(3).gameObject;
        ships = map_display.transform.GetChild(4).gameObject;

        StartCoroutine(mapUpdater());    
    }

    //clears all non-default items from the map
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
        float scale_adjustment = 1.0f + ((1.0f - zoom_percentage) * 1.0f);

        for (int i = 0; i < corresponding_icons.Length; i++)
        {
            float x_coordinate = corresponding_locations[i].x * (0.0005f + ((zoom_percentage) * 0.0005f));
            float z_coordinate = corresponding_locations[i].z * (0.0005f + ((zoom_percentage) * 0.0005f));
            corresponding_icons[i].transform.localPosition =
                new Vector3(x_coordinate,
                            z_coordinate,
                            0.0f);

            MapItem item_info = map_items[i].GetComponent<MapItem>();
            corresponding_icons[i].GetComponent<RectTransform>().sizeDelta = new Vector2(item_info.getSize() * 0.01f / scale_adjustment, item_info.getSize() * 0.01f / scale_adjustment);
        }

    }

    public void rotateMap()
    {
        natural_phenomena.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, this_ship.transform.localEulerAngles.y);
        ships.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, this_ship.transform.localEulerAngles.y);
    }

    private void updateMap()
    {
        map_items = GameObject.FindGameObjectsWithTag("MapItem");
        corresponding_locations = new Vector3[map_items.Length];
        corresponding_icons = new GameObject[map_items.Length];

        for (int i = 0; i < map_items.Length; i++)
        {
            corresponding_locations[i] =
                new Vector3(map_items[i].transform.position.x,
                            this_ship.transform.position.y,
                            map_items[i].transform.position.z);

            if (Vector3.Distance(this_ship.transform.position, corresponding_locations[i]) < 500.0f)
            {
                GameObject item_to_add = null;
                MapItem item_info = map_items[i].GetComponent<MapItem>();
                bool item_is_ship = item_info.isShip();
                if (item_is_ship == true)
                {
                    item_to_add = GameObject.Instantiate(ships.transform.GetChild(0).gameObject, ships.transform);
                }
                else
                {
                    item_to_add = GameObject.Instantiate(natural_phenomena.transform.GetChild(0).gameObject, ships.transform);
                }

                corresponding_locations[i] -= this_ship.transform.position;

                item_to_add.transform.localPosition =
                    new Vector3(corresponding_locations[i].x * 0.001f,
                                corresponding_locations[i].z * 0.001f,
                                0.0f);

                item_to_add.SetActive(true);
                corresponding_icons[i] = item_to_add;
            }
        }

        zoomMap();
    }


    IEnumerator mapUpdater()
    {
        while (true)
        {
            resetMap();
            updateMap();
            yield return null;
            //yield return new WaitForSeconds(MAP_UPDATE_DELAY);
        }
    }
}
