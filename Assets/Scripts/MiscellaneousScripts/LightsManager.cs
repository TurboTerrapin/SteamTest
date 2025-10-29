/*
    LightsManager.cs
    - Handles light stuff
    Contributor(s): Jake Schott
    Last Updated: 10/23/2025
*/

using System.Collections;
using UnityEngine;

public class LightsManager : MonoBehaviour
{
    //CLASS CONSTANTS (0 IS DEFAULT, 1 IS EMERGENCY LIGHTS)
    private static float[] LIGHT_CHANGE_TIME = new float[2] { 0.5f, 0.5f };
    private static float[] DEFAULT_LIGHT_INTENSITY = new float[2] { 20.0f, 10.0f };
    private static Color[] DEFAULT_LIGHT_COLOR = new Color[] { new Color(0.22f, 0.80f, 0.97f), new Color(0.59f, 0.86f, 0.96f)};
    private static Material[] DEFAULT_LIGHT_MATERIAL = new Material[2] { null, null };
    private static float[] RED_ALERT_LIGHT_INTENSITY = new float[2] { 10.0f, 10.0f };
    private static Color[] RED_ALERT_LIGHT_COLOR = new Color[] { new Color(1.0f, 0.0f, 0.0f), new Color(0.8f, 0.02f, 0.0f)};

    public Material lit_neon;
    public Material unlit_neon;
    public Material lit_off_white;
    public Material lit_red;

    private GameObject[] light_groups = new GameObject[2] { null, null };
    private ShipStatus ship_status;

    private bool[] enabled_lights = new bool[2] { true, false };
    private Coroutine[] light_change_coroutines = new Coroutine[2] { null, null };

    private void Start()
    {
        light_groups[0] = transform.GetChild(0).gameObject;
        light_groups[1] = transform.GetChild(1).gameObject;

        DEFAULT_LIGHT_MATERIAL[0] = lit_neon;
        DEFAULT_LIGHT_MATERIAL[1] = lit_off_white;

        ship_status = GameObject.FindGameObjectWithTag("ControlHandler").GetComponent<ShipStatus>();
    }

    //helper method that changes the intensity of 

    //helper method that changes the materials of every physical .FBX parented to a light source
    private void changeMaterials(Transform light_group, Material physical_light_material)
    {
        foreach (Transform light in light_group) //iterate through every (potential) FBX model
        {
            foreach (Transform physical_light in light.transform)
            {
                physical_light.GetComponent<Renderer>().material = physical_light_material; //set material
            }
        }
    }

    //used to set the lights to their default color and material, called by ScenarioManager.controlResetHelper()
    public void resetLights()
    {
        for (int i = 0; i < 2; i++)
        {
            if (light_change_coroutines[i] != null)
            {
                StopCoroutine(light_change_coroutines[i]);
                light_change_coroutines[i] = null;
            }
        }

        //enable default lights
        changeLightColors(light_groups[0].transform, DEFAULT_LIGHT_COLOR[0]);
        changeLightIntensities(light_groups[0].transform, DEFAULT_LIGHT_INTENSITY[0]);
        changeMaterials(light_groups[0].transform, lit_neon);

        //disable emergency lights
        changeLightColors(light_groups[1].transform, DEFAULT_LIGHT_COLOR[1]);
        changeLightIntensities(light_groups[1].transform, 0.0f);
        changeMaterials(light_groups[1].transform, unlit_neon);

        //default lights enabled to start, emergency lights disabled to start
        enabled_lights[0] = true;
        enabled_lights[1] = false;
    }

    //helper method that changes every light's color in light_group
    private void changeLightColors(Transform light_group, Color light_color)
    {
        foreach (Transform light in light_group) //iterate through every light
        {
            light.GetComponent<Light>().color = light_color;
        }
    }

    //helper method that changes every light's color and intensity in light_group
    private void changeLightIntensities(Transform light_group, float light_intensity)
    {
        foreach (Transform light in light_group) //iterate through every light
        {
            light.GetComponent<Light>().intensity = light_intensity;
        }
    }

    //used to dim/brighten a set of lights (does not affect their materials)
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

    //used to enable/disable a set of lights (calls lightIntensityChange() and changeMaterials)
    IEnumerator lightsChange(int index, float desired_intensity)
    {
        Transform light_group = light_groups[index].transform;

        if (desired_intensity > 0.0f)
        {
            if (ship_status.getCurrColor() != 2)
            {
                changeMaterials(light_group, DEFAULT_LIGHT_MATERIAL[index]);
            }
            else
            {
                changeMaterials(light_group, lit_red);
            }
        }

        Light[] lights_to_adjust = new Light[light_group.transform.childCount];
        for (int i = 0; i < lights_to_adjust.Length; i++)
        {
            lights_to_adjust[i] = light_group.transform.GetChild(i).GetComponent<Light>();
        }
        yield return lightIntensityChange(lights_to_adjust, LIGHT_CHANGE_TIME[index], desired_intensity);

        if (desired_intensity == 0.0f)
        {
            changeMaterials(light_group, unlit_neon);
        }

        light_change_coroutines[index] = null;
    }

    private void resetLightChangeCoroutine(int index)
    {
        if (light_change_coroutines[index] != null)
        {
            StopCoroutine(light_change_coroutines[index]);
        }
    }

    public void enableEmergencyLights()
    {
        resetLightChangeCoroutine(1);
        enabled_lights[1] = true;
        light_change_coroutines[1] = StartCoroutine(lightsChange(1, DEFAULT_LIGHT_INTENSITY[1]));
    }

    public void disableEmergencyLights()
    {
        resetLightChangeCoroutine(1);
        enabled_lights[1] = false;
        light_change_coroutines[1] = StartCoroutine(lightsChange(1, 0.0f));
    }

    public void enableDefaultLights()
    {
        resetLightChangeCoroutine(0);
        enabled_lights[0] = true;
        light_change_coroutines[0] = StartCoroutine(lightsChange(0, DEFAULT_LIGHT_INTENSITY[0]));
    }  

    public void disableDefaultLights()
    {
        resetLightChangeCoroutine(0);
        enabled_lights[0] = false;
        light_change_coroutines[0] = StartCoroutine(lightsChange(0, 0.0f));
    }

    public void enableRedAlert()
    {
        for (int i = 0; i < 2; i++)
        {
            if (enabled_lights[i] == true)
            {
                resetLightChangeCoroutine(i);
                changeMaterials(light_groups[i].transform, lit_red);
                changeLightIntensities(light_groups[i].transform, RED_ALERT_LIGHT_INTENSITY[i]);
            }
            changeLightColors(light_groups[i].transform, RED_ALERT_LIGHT_COLOR[i]);
        }
    }

    public void disableRedAlert()
    {
        for (int i = 0; i < 2; i++)
        {
            if (enabled_lights[i] == true)
            {
                resetLightChangeCoroutine(i);
                changeMaterials(light_groups[i].transform, DEFAULT_LIGHT_MATERIAL[i]);
                changeLightIntensities(light_groups[i].transform, DEFAULT_LIGHT_INTENSITY[i]);
            }
            changeLightColors(light_groups[i].transform, DEFAULT_LIGHT_COLOR[i]);
        }
    }
}