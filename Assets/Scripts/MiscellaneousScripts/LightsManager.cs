/*
    LightsManager.cs
    - Handles light stuff
    Contributor(s): Jake Schott
    Last Updated: 9/4/2025
*/

using UnityEngine;

public class LightsManager : MonoBehaviour
{
    //CLASS CONSTANTS
    private const float DEFAULT_LIGHT_INTENSITY = 20.0f;
    private static Color DEFAULT_LIGHT_COLOR = new Color(0.22f, 0.80f, 0.97f);
    private const float RED_ALERT_LIGHT_INTENSITY = 5.0f;
    private static Color RED_ALERT_LIGHT_COLOR = new Color(1.0f, 0.0f, 0.0f);

    public Material lit_neon;
    public Material unlit_neon;
    public Material lit_red;

    private GameObject default_lights;
    private GameObject emergency_lights; //ignore for now

    private void Start()
    {
        default_lights = transform.GetChild(0).gameObject;
        emergency_lights = transform.GetChild(1).gameObject;
    }

    //helper method that changes every default light's color, intensity, and material
    private void changeAllDefaultLights(Color light_color, float light_intensity, Material physical_light_material)
    {
        foreach (Transform light in default_lights.transform) //iterate through every default light
        {
            light.GetComponent<Light>().color = light_color;
            light.GetComponent<Light>().intensity = light_intensity;
            foreach (Transform physical_light in light.transform) //iterate through every (potential) FBX model
            {
                physical_light.GetComponent<Renderer>().material = physical_light_material; //set material
            }
        }
    }

    public void enableDefaultLights()
    {
        changeAllDefaultLights(DEFAULT_LIGHT_COLOR, DEFAULT_LIGHT_INTENSITY, lit_neon);
    }  

    public void disableDefaultLights()
    {
        changeAllDefaultLights(DEFAULT_LIGHT_COLOR, 0.0f, unlit_neon);
    }

    public void enableRedAlert()
    {
        changeAllDefaultLights(RED_ALERT_LIGHT_COLOR, RED_ALERT_LIGHT_INTENSITY, lit_red);
    }

    public void disableRedAlert()
    {
        changeAllDefaultLights(DEFAULT_LIGHT_COLOR, DEFAULT_LIGHT_INTENSITY, lit_neon); //same as enableDefaultLights for the moment
    }
}
