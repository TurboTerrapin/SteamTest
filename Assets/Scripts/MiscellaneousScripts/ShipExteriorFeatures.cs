/*
    ShipExteriorFeatures.cs
    - Handles rotating elements and blinking lights on the exterior of the ship
    Contributor(s): Jake Schott
    Last Updated: 4/11/2026
*/

using System.Collections;
using UnityEngine;

public class ShipExteriorFeatures : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float FEATURE_ROTATION_SPEED = 30.0f;
    private static float LIGHT_BLINK_DELAY = 0.5f;

    public GameObject ship_engine_circles;
    public GameObject ship_radar_dish;
    public GameObject ship_white_lights;
    public Material lit_white;
    public Material black;

    private void Start()
    {
        StartCoroutine(lightBlinker());
    }

    private void Update()
    {
        float rotation = Time.deltaTime * FEATURE_ROTATION_SPEED;
        ship_engine_circles.transform.Rotate(rotation, 0.0f, 0.0f);
        ship_radar_dish.transform.Rotate(0.0f, 0.0f, rotation);
    }

    IEnumerator lightBlinker()
    {
        while (true)
        {
            ship_white_lights.GetComponent<Renderer>().material = lit_white;
            yield return new WaitForSeconds(LIGHT_BLINK_DELAY);
            ship_white_lights.GetComponent<Renderer>().material = black;
            yield return new WaitForSeconds(LIGHT_BLINK_DELAY);
        }
    }
}
