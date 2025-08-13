/*
    EngineerMap.cs
    - Handles engineer map
    Contributor(s): Jake Schott
    Last Updated: 8/11/2025
*/

using TMPro;
using UnityEngine;

public class EngineerMap : MonoBehaviour
{
    private GameObject this_ship;
    private GameObject world_root;
    public GameObject navigation_information;
    public GameObject altitude_label;
    public GameObject chungus;

    private GameObject entrance_path;
    private GameObject exit_path;
    private GameObject ship_icon;

    void Start()
    {
        this_ship = GameObject.FindGameObjectWithTag("Spaceship");
        world_root = GameObject.FindGameObjectWithTag("WorldRoot");

        entrance_path = navigation_information.transform.GetChild(0).gameObject;
        exit_path = navigation_information.transform.GetChild(1).gameObject;
        ship_icon = navigation_information.transform.GetChild(2).gameObject;
    }

    public void plotPoint(Vector2 p)
    {
        p *= (0.265f / (ScenarioManager.BOUNDARY_SIZE * 0.5f));
        GameObject to_place = Object.Instantiate(chungus, chungus.transform.parent);
        to_place.transform.localPosition = new Vector3(p.x, p.y, 0.0f);
        to_place.SetActive(true);
    }

    public void updatePathLocations(Vector2 ent_path_pos, float ent_rot, Vector2 exit_path_pos, float exit_rot)
    {
        ent_path_pos *= (0.265f / (ScenarioManager.BOUNDARY_SIZE * 0.5f));
        exit_path_pos *= (0.265f / (ScenarioManager.BOUNDARY_SIZE * 0.5f));
        entrance_path.transform.localPosition = new Vector3(ent_path_pos.x, ent_path_pos.y, 0.0f);
        entrance_path.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -90.0f + ent_rot);
        exit_path.transform.localPosition = new Vector3(exit_path_pos.x, exit_path_pos.y, 0.0f);
        exit_path.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -90.0f + exit_rot);
    }

    public void updateShipOrientation(float ship_rotation)
    {
        ship_icon.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, ship_rotation);
    }

    public void updateShipLocation(Vector2 ship_location)
    {
        ship_location *= (0.265f / (ScenarioManager.BOUNDARY_SIZE * 0.5f));
        ship_icon.transform.localPosition = new Vector3(ship_location.x, ship_location.y, 0.0f);
    }

    public void updateAltitude(float new_altitude)
    {
        string rounded_altitude = (Mathf.Round(new_altitude * 10.0f) / 10.0f).ToString();
        if (rounded_altitude.Contains(".") == false)
        {
            rounded_altitude += ".0";
        }
        altitude_label.GetComponent<TMP_Text>().SetText("ALTITUDE: " + rounded_altitude + "m");
    }
}
