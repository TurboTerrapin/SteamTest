/*
    ShuttleExteriorFeatures.cs
    - Handles rotating elements and blinking lights on the exterior of a shuttle
    Contributor(s): Jake Schott
    Last Updated: 5/22/2026
*/

using System.Collections;
using UnityEngine;

public class ShuttleExteriorFeatures : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float FEATURE_ROTATION_SPEED = 45.0f;
    private static float LIGHT_BLINK_DELAY = 0.2f;

    [SerializeField]
    private bool enable_cosmetic_features = true;

    public GameObject shuttle_engine_circles;
    public GameObject shuttle_radar_dish;
    public GameObject shuttle_white_lights;
    public Material lit_white;
    public Material black;

    private Material[] emergency_light_blue_materials;
    private Material[] emergency_light_red_materials;

    private void Start()
    {
        if (enable_cosmetic_features == true)
        {
            emergency_light_blue_materials = shuttle_radar_dish.GetComponent<Renderer>().materials;
            emergency_light_blue_materials[2] = black;
            emergency_light_red_materials = shuttle_radar_dish.GetComponent<Renderer>().materials;
            emergency_light_red_materials[1] = black;
            StartCoroutine(lightBlinker());
            StartCoroutine(rotator());
        }
    }

    IEnumerator rotator()
    {
        while (true)
        {
            float rotation = Time.deltaTime * FEATURE_ROTATION_SPEED;
            shuttle_engine_circles.transform.Rotate(rotation, 0.0f, 0.0f);
            shuttle_radar_dish.transform.Rotate(0.0f, 0.0f, rotation);
            yield return null;
        }
    }

    IEnumerator lightBlinker()
    {
        while (true)
        {
            shuttle_white_lights.GetComponent<Renderer>().material = lit_white;
            shuttle_radar_dish.GetComponent<Renderer>().materials = emergency_light_red_materials;
            yield return new WaitForSeconds(LIGHT_BLINK_DELAY);
            shuttle_radar_dish.GetComponent<Renderer>().materials = emergency_light_blue_materials;
            yield return new WaitForSeconds(LIGHT_BLINK_DELAY);
            shuttle_radar_dish.GetComponent<Renderer>().materials = emergency_light_red_materials;
            shuttle_white_lights.GetComponent<Renderer>().material = black;
            yield return new WaitForSeconds(LIGHT_BLINK_DELAY);
            shuttle_radar_dish.GetComponent<Renderer>().materials = emergency_light_blue_materials;
            yield return new WaitForSeconds(LIGHT_BLINK_DELAY);
        }
    }
}
