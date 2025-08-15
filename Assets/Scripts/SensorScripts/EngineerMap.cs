/*
    EngineerMap.cs
    - Handles engineer map
    Contributor(s): Jake Schott
    Last Updated: 8/14/2025
*/

using System.Collections;
using TMPro;
using UnityEngine;

public class EngineerMap : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float FLASH_SPEED = 0.5f;
    private static Color TMP_RED = new Color(1.0f, 0.0f, 0.0f, 1.0f);
    private static Color SPRITE_RED = new Color(0.96f, 0.25f, 0.28f, 1.0f); //sprite renderer shows color differently

    public GameObject navigation_canvas;
    public GameObject navigation_information;
    public GameObject altitude_label;

    private GameObject entrance_path;
    private GameObject exit_path;
    private GameObject ship_icon;
    private GameObject circle_boundary;
    private GameObject countdown;

    private Coroutine red_flash_coroutine = null;

    void Start()
    {
        entrance_path = navigation_information.transform.GetChild(0).gameObject;
        exit_path = navigation_information.transform.GetChild(1).gameObject;
        ship_icon = navigation_information.transform.GetChild(2).gameObject;
        circle_boundary = navigation_information.transform.GetChild(3).gameObject;
        countdown = circle_boundary.transform.GetChild(0).gameObject;
    }

    private void colorChange(Color to_change_to)
    {
        circle_boundary.GetComponent<SpriteRenderer>().color = to_change_to;

        GameObject[] paths = new GameObject[2] { entrance_path, exit_path };
        for (int i = 0; i < paths.Length; i++)
        {
            paths[i].transform.GetChild(0).GetComponent<SpriteRenderer>().color = to_change_to;
            paths[i].transform.GetChild(1).GetComponent<SpriteRenderer>().color = to_change_to;
        }
    }

    IEnumerator redLightFlasher()
    {
        countdown.GetComponent<TMP_Text>().color = new Color(TMP_RED.r, TMP_RED.g, TMP_RED.b, 1.0f);
        colorChange(SPRITE_RED);
        while (true)
        {
            for (int i = 0; i < 2; i++)
            {
                float anim_time = FLASH_SPEED;
                while (anim_time > 0.0f)
                {
                    anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
                    if (i == 0)
                    {
                        colorChange(new Color(SPRITE_RED.r, SPRITE_RED.g, SPRITE_RED.b, 1.0f - (0.45f * (1.0f - (anim_time / FLASH_SPEED)))));
                    }
                    else
                    {
                        colorChange(new Color(SPRITE_RED.r, SPRITE_RED.g, SPRITE_RED.b, 0.45f + (0.55f * (1.0f - (anim_time / FLASH_SPEED)))));
                    }
                    yield return null;
                } 
            }
        }
    }

    public void updateAltitudeWarning(bool active, string msg)
    {
        navigation_canvas.transform.GetChild(3).GetChild(0).GetComponent<TMP_Text>().SetText("! " + msg + " ALTITUDE !");
        navigation_canvas.transform.GetChild(3).GetChild(0).gameObject.SetActive(active);
        if (active == true)
        {
            altitude_label.GetComponent<TMP_Text>().color = TMP_RED;
        }
        else
        {
            altitude_label.GetComponent<TMP_Text>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        }
    }

    public void updateShipBoundaryStatus(bool inside_boundary)
    {
        if (inside_boundary == true)
        {
            if (red_flash_coroutine != null)
            {
                StopCoroutine(red_flash_coroutine);
                red_flash_coroutine = null;
            }
            //color sprite renderers
            colorChange(new Color(0.0f, 0.84f, 1.0f));
            //resize ship pointer
            ship_icon.transform.GetChild(0).localScale = new Vector3(0.01f, 0.01f, 1.0f);
            //color navigation label
            navigation_canvas.transform.GetChild(1).GetComponent<TMP_Text>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
            //color navigation bar
            navigation_canvas.transform.GetChild(2).GetComponent<UnityEngine.UI.Image>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
            //altitude/impulse bars
            navigation_canvas.transform.GetChild(5).transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
            navigation_canvas.transform.GetChild(5).transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
            //hide countdown
            countdown.SetActive(false);
            //detection warnings
            navigation_canvas.transform.GetChild(3).gameObject.SetActive(false);
        }
        else
        {
            if (red_flash_coroutine == null)
            {
                red_flash_coroutine = StartCoroutine(redLightFlasher());
            }
            //resize ship pointer
            ship_icon.transform.GetChild(0).localScale = new Vector3(0.02f, 0.02f, 1.0f);
            //color navigation label
            navigation_canvas.transform.GetChild(1).GetComponent<TMP_Text>().color = new Color(TMP_RED.r, TMP_RED.g, TMP_RED.b, 1.0f);
            //color navigation bar
            navigation_canvas.transform.GetChild(2).GetComponent<UnityEngine.UI.Image>().color = new Color(TMP_RED.r, TMP_RED.g, TMP_RED.b, 1.0f);
            //altitude/impulse bars
            navigation_canvas.transform.GetChild(5).transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color = TMP_RED;
            navigation_canvas.transform.GetChild(5).transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().color = TMP_RED;
            //show countdown
            countdown.SetActive(true);
            //detection warnings
            navigation_canvas.transform.GetChild(3).gameObject.SetActive(true);
        }
    }

    public void updateShipBoundaryCountdownStatus(int countdown_value)
    {
        countdown.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        if (countdown_value >= 10 && countdown_value < 20)
        {
            countdown.transform.localPosition = new Vector3(0.0f, -0.34f, 0.0f);
        }
        countdown.SetActive(true);
        countdown.GetComponent<TMP_Text>().text = countdown_value.ToString();
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
        if (ship_location.y <= -0.285f)
        {
            ship_icon.transform.GetChild(0).GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        }
        else
        {
            ship_icon.transform.GetChild(0).GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.None;
        }
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