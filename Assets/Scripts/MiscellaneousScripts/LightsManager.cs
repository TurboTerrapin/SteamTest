/*
    LightsManager.cs
    - Handles light stuff
    Contributor(s): Jake Schott
    Last Updated: 9/7/2025
*/

using System.Collections;
using UnityEngine;

public class LightsManager : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float LIGHT_CHANGE_TIME = 0.5f; //half a second
    private const float DEFAULT_LIGHT_INTENSITY = 20.0f;
    private static Color DEFAULT_LIGHT_COLOR = new Color(0.22f, 0.80f, 0.97f);
    private const float RED_ALERT_LIGHT_INTENSITY = 5.0f;
    private static Color RED_ALERT_LIGHT_COLOR = new Color(1.0f, 0.0f, 0.0f);

    public Material lit_neon;
    public Material unlit_neon;
    public Material lit_red;

    private GameObject default_lights;
    private GameObject emergency_lights; //ignore for now

    private Coroutine default_light_change_coroutine = null;

    private void Start()
    {
        default_lights = transform.GetChild(0).gameObject;
        emergency_lights = transform.GetChild(1).gameObject;
    }

    private void changeAllDefaultLightsMaterial(Material physical_light_material)
    {
        foreach (Transform light in default_lights.transform) //iterate through every (potential) FBX model
        {
            foreach (Transform physical_light in light.transform)
            {
                physical_light.GetComponent<Renderer>().material = physical_light_material; //set material
            }
        }
    }

    //used to set the lights to their default color and material, called by ScenarioManager.Reset
    public void resetLights()
    {
        if (default_light_change_coroutine != null)
        {
            StopCoroutine(default_light_change_coroutine);
            default_light_change_coroutine = null;
        }

        changeAllDefaultLights(DEFAULT_LIGHT_COLOR, DEFAULT_LIGHT_INTENSITY);
        changeAllDefaultLightsMaterial(lit_neon);
    }

    //helper method that changes every default light's color and intensity
    private void changeAllDefaultLights(Color light_color, float light_intensity)
    {
        foreach (Transform light in default_lights.transform) //iterate through every default light
        {
            light.GetComponent<Light>().color = light_color;
            light.GetComponent<Light>().intensity = light_intensity;
        }
    }

    IEnumerator lightIntensityChange(Light[] lights_to_adjust, float time, float to_change_to)
    {
        float anim_time = time;

        float[] starting_intensities = new float[lights_to_adjust.Length];
        for (int i = 0; i < lights_to_adjust.Length; i++)
        {
            starting_intensities[i] = lights_to_adjust[i].intensity;
        }

        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            for (int i = 0; i < lights_to_adjust.Length; i++)
            {
                lights_to_adjust[i].intensity = Mathf.Lerp(starting_intensities[i], to_change_to, 1.0f - (anim_time / time));
            }

            yield return null;
        }
    }

    IEnumerator allDefaultLightsChange(float intensity)
    {
        if (intensity > 0.0f)
        {
            changeAllDefaultLightsMaterial(lit_neon);
        }

        Light[] lights_to_adjust = new Light[default_lights.transform.childCount];
        for (int i = 0; i < lights_to_adjust.Length; i++)
        {
            lights_to_adjust[i] = default_lights.transform.GetChild(i).GetComponent<Light>();
        }
        yield return lightIntensityChange(lights_to_adjust, LIGHT_CHANGE_TIME, intensity);

        if (intensity == 0.0f)
        {
            changeAllDefaultLightsMaterial(unlit_neon);
        }

        default_light_change_coroutine = null;
    }

    public void enableDefaultLights()
    {
        if (default_light_change_coroutine != null)
        {
            StopCoroutine(default_light_change_coroutine);
        }

        default_light_change_coroutine = StartCoroutine(allDefaultLightsChange(DEFAULT_LIGHT_INTENSITY));
    }  

    public void disableDefaultLights()
    {
        if (default_light_change_coroutine != null)
        {
            StopCoroutine(default_light_change_coroutine);

        }

        default_light_change_coroutine = StartCoroutine(allDefaultLightsChange(0.0f));
    }

    public void enableRedAlert()
    {
        changeAllDefaultLights(RED_ALERT_LIGHT_COLOR, RED_ALERT_LIGHT_INTENSITY);
        changeAllDefaultLightsMaterial(lit_red);
    }

    public void disableRedAlert()
    {
        changeAllDefaultLights(DEFAULT_LIGHT_COLOR, DEFAULT_LIGHT_INTENSITY);
        changeAllDefaultLightsMaterial(lit_neon);
    }
}
