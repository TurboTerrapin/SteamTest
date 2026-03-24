/*
    LightsManager.cs
    - Handles light stuff
    Contributor(s): Jake Schott, Henryk Musial
    Last Updated: 3/16/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightsManager : MonoBehaviour
{
    //CLASS CONSTANTS (0 IS DEFAULT, 1 IS EMERGENCY LIGHTS)
    private static float LIGHT_CHANGE_TIME = 0.5f;
    private static float[] DEFAULT_LIGHT_INTENSITIES = new float[2] { 5.0f, 10.0f };
    private static Color[] DEFAULT_LIGHT_COLORS = new Color[] { new Color(0.66f, 0.92f, 1.0f), new Color(0.87f, 0.96f, 1.0f) };
    private static float[] RED_ALERT_LIGHT_INTENSITIES = new float[2] { 10.0f, 10.0f };
    private static Color[] RED_ALERT_LIGHT_COLORS = new Color[] { new Color(1.0f, 0.0f, 0.0f), new Color(0.8f, 0.02f, 0.0f) };
    //order of lights in default lights hierarchy (ex. transform.GetChild(11) would get an aft light)
    private static int[][] DEFAULT_DIRECTIONAL_INDEXES = new int[][]
    {
        new int[]{ 0, 1, 2, 3, 4 }, //forward lights
        new int[]{ 5, 6, 7 }, //port lights
        new int[]{ 8, 9, 10 }, //starboard lights
        new int[]{ 11, 12, 13 } //aft lights
    };
    //order of secondary lit objects in lit elements hierarchy
    private static int[][] LIT_DIRECTIONAL_INDEXES = new int[][]
{
        new int[]{ 0, 1, 2, 3, 4, 5, 6, 7, 8 }, //forward lit elements
        new int[]{ 9 }, //port lit elements
        new int[]{ 10 }, //starboard lit elements
        new int[]{ 11 } //aft lit elements
};

    public GameObject default_light_group;
    public GameObject emergency_light_group;
    public GameObject lit_element_group;

    private List<Light>[] default_lights = new List<Light>[4];
    private List<Renderer>[] default_renderers = new List<Renderer>[4];
    private List<Renderer>[] lit_renderers = new List<Renderer>[4];
    private List<Light> emergency_lights = new List<Light>();
    private List<Renderer> emergency_renderers = new List<Renderer>();
    private ShipStatus ship_status;

    private bool[] enabled_lights = new bool[2] { true, false }; //default, emergency
    private float[] light_intensities = new float[] { DEFAULT_LIGHT_INTENSITIES[0], 0.0f }; //default, emergency

    //below correspond to forward, port, starboard, aft
    private Color[] current_default_colors = new Color[4];
    private Material[] current_default_materials = new Material[4];
    private Color[] normal_default_colors = new Color[4];
    private Material[] normal_default_materials = new Material[4];
    private Color emergency_color;
    private Material emergency_material;

    //below correspond to forward, port, starboard, aft
    private float[] flicker_times = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f };
    private Material[] flicker_materials = new Material[4];
    private Coroutine[] flicker_coroutines = new Coroutine[4] { null, null, null, null };

    private Coroutine[] light_change_coroutines = new Coroutine[2] { null, null }; //default, emergency

    private void Start()
    {
        //add forward, port, starboard, and aft lights and lit elements
        for (int i = 0; i < 4; i++)
        {
            default_lights[i] = new List<Light>();
            default_renderers[i] = new List<Renderer>();
            lit_renderers[i] = new List<Renderer>();
            for (int k = 0; k < DEFAULT_DIRECTIONAL_INDEXES[i].Length; k++)
            {
                default_lights[i].Add(default_light_group.transform.GetChild(DEFAULT_DIRECTIONAL_INDEXES[i][k]).GetComponent<Light>());
                default_renderers[i].Add(default_light_group.transform.GetChild(DEFAULT_DIRECTIONAL_INDEXES[i][k]).GetChild(0).GetComponent<Renderer>());
            }
            for (int k = 0; k < LIT_DIRECTIONAL_INDEXES[i].Length; k++)
            {
                lit_renderers[i].Add(lit_element_group.transform.GetChild(LIT_DIRECTIONAL_INDEXES[i][k]).GetComponent<Renderer>());
            }

            flicker_materials[i] = new Material(ReferenceAssistor.Instance.lit_neon);
        }
        
        //add emergency lights
        for (int i = 0; i < emergency_light_group.transform.childCount; i++)
        {
            emergency_lights.Add(emergency_light_group.transform.GetChild(i).GetComponent<Light>());
            emergency_renderers.Add(emergency_light_group.transform.GetChild(i).GetChild(0).GetComponent<Renderer>());
        }
        emergency_material = ReferenceAssistor.Instance.unlit_neon;

        ship_status = ReferenceAssistor.Instance.module_handlers[3].GetComponent<ShipStatus>();

        resetLights();
    }

    //cleanup runtime materials
    private void OnDestroy()
    {
        for (int i = 0; i < 4; i++)
        {
            Destroy(flicker_materials[i]);
        }
    }

    public void changeSectionAppearance(int section, Material lit_material, Color light_color)
    {
        normal_default_colors[section] = light_color;
        normal_default_materials[section] = lit_material;
        if (ship_status.getCurrColor() < 2)
        {
            current_default_colors[section] = normal_default_colors[section];
            current_default_materials[section] = normal_default_materials[section];
        }
        displayLightColors(0);
        displayLightMaterials(0);
    }

    private void displayLightMaterials(int index)
    {
        if (index == 0) //default
        {
            for (int i = 0; i < 4; i++)
            {
                for (int k = 0; k < default_renderers[i].Count; k++)
                {
                    if (enabled_lights[index] == true)
                    {
                        default_renderers[i][k].material = current_default_materials[i];
                    }
                    else
                    {
                        default_renderers[i][k].material = ReferenceAssistor.Instance.unlit_neon;
                    }
                }
                for (int k = 0; k < lit_renderers[i].Count; k++)
                {
                    if (enabled_lights[index] == true)
                    {
                        lit_renderers[i][k].material = current_default_materials[i];
                    }
                    else
                    {
                        lit_renderers[i][k].material = ReferenceAssistor.Instance.pure_black;
                    }
                }
            }
        }
        else //emergency
        {
            for (int i = 0; i < emergency_renderers.Count; i++)
            {
                if (enabled_lights[index] == true)
                {
                    emergency_renderers[i].material = emergency_material;
                }
                else
                {
                    emergency_renderers[i].material = ReferenceAssistor.Instance.unlit_neon;
                }
            }
        }
    }

    private void displayLightIntensities(int index)
    {
        if (enabled_lights[index] == false)
        {
            return;
        }

        if (index == 0) //default
        {
            for (int i = 0; i < 4; i++)
            {
                for (int k = 0; k < default_lights[i].Count; k++)
                {
                    default_lights[i][k].intensity = light_intensities[index];
                }
            }
        }
        else //emergency
        {
            for (int i = 0; i < emergency_lights.Count; i++)
            {
                emergency_lights[i].intensity = light_intensities[index];
            }
        }
    }

    private void displayLightColors(int index)
    {
        if (index == 0) //default
        {
            for (int i = 0; i < 4; i++)
            {
                for (int k = 0; k < default_lights[i].Count; k++)
                {
                    default_lights[i][k].color = current_default_colors[i];
                }
            }
        }
        else //emergency
        {
            for (int i = 0; i < emergency_lights.Count; i++)
            {
                emergency_lights[i].color = emergency_color;
            }
        }
    }

    //called on initialization, scenario transition
    public void resetLights()
    {
        //stop light change coroutines
        for (int i = 0; i < 2; i++)
        {
            endLightChangeCoroutine(i);
        }

        //stop flicker coroutines
        endFlickerCoroutines();

        //default lights enabled to start, emergency lights disabled to start
        enabled_lights[0] = true;
        light_intensities[0] = DEFAULT_LIGHT_INTENSITIES[0];
        enabled_lights[1] = false;
        light_intensities[1] = 0.0f;

        //enable default lights
        for (int i = 0; i < 4; i++)
        {
            current_default_colors[i] = DEFAULT_LIGHT_COLORS[0];
            normal_default_colors[i] = DEFAULT_LIGHT_COLORS[0];
            current_default_materials[i] = ReferenceAssistor.Instance.lit_neon;
            normal_default_materials[i] = ReferenceAssistor.Instance.lit_neon;
        }

        //disable emergency lights
        emergency_color = DEFAULT_LIGHT_COLORS[1];
        emergency_material = ReferenceAssistor.Instance.unlit_neon;

        //push updates
        for (int i = 0; i < 2; i++)
        {
            displayLightColors(i);
            displayLightIntensities(i);
            displayLightMaterials(i);
        }
    }

    //used to dim/brighten a set of lights (does not affect their materials)
    IEnumerator lightIntensityChange(int index, float time, float to_change_to)
    {
        float anim_time = time;

        List<Light> lights_to_adjust = new List<Light>();
        if (index == 0) //default
        {
            for (int i = 0; i < 4; i++)
            {
                for (int k = 0; k < default_lights[i].Count; k++)
                {
                    lights_to_adjust.Add(default_lights[i][k]);
                }
            }
        }
        else //emergency
        {
            lights_to_adjust = emergency_lights;
        }

        float[] starting_intensities = new float[lights_to_adjust.Count];
        for (int i = 0; i < lights_to_adjust.Count; i++)
        {
            starting_intensities[i] = lights_to_adjust[i].intensity;
        }

        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            for (int i = 0; i < lights_to_adjust.Count; i++)
            {
                lights_to_adjust[i].intensity = Mathf.Lerp(starting_intensities[i], to_change_to, 1.0f - (anim_time / time));
            }

            yield return null;
        }
    }

    //used to enable/disable a set of lights (calls lightIntensityChange() and changeMaterials()
    IEnumerator lightsChange(int index, float desired_intensity)
    {
        if (desired_intensity > 0.0f) //turning on
        {
            if (index == 0) //default
            {
                for (int i = 0; i < 4; i++)
                {
                    current_default_materials[i] = normal_default_materials[i];
                }
            }
            else //emergency
            {
                if (ship_status.getCurrColor() < 2)
                {
                    emergency_material = ReferenceAssistor.Instance.lit_off_white;
                }
                else
                {
                    emergency_material = ReferenceAssistor.Instance.lit_red;
                }
            }
        }
        displayLightMaterials(index);

        yield return lightIntensityChange(index, LIGHT_CHANGE_TIME, desired_intensity);

        if (desired_intensity == 0.0f)
        {
            if (index == 0) //default
            {
                for (int i = 0; i < 4; i++)
                {
                    current_default_materials[i] = ReferenceAssistor.Instance.unlit_neon;
                }
            }
            else //emergency
            {
                emergency_material = ReferenceAssistor.Instance.unlit_neon;
            }
        }
        displayLightMaterials(index);

        light_change_coroutines[index] = null;
    }

    private void endLightChangeCoroutine(int index)
    {
        if (light_change_coroutines[index] != null)
        {
            StopCoroutine(light_change_coroutines[index]);
            light_change_coroutines[index] = null;
        }
    }

    //ends all flicker coroutines
    private void endFlickerCoroutines()
    {
        for (int i = 0; i < 4; i++)
        {
            if (flicker_coroutines[i] != null)
            {
                StopCoroutine(flicker_coroutines[i]);
                flicker_coroutines[i] = null;
            }
        }
    }

    public void setDefaultLights(bool active)
    {
        endFlickerCoroutines();
        endLightChangeCoroutine(0);
        enabled_lights[0] = active;
        float intensity_to_change_to = DEFAULT_LIGHT_INTENSITIES[0];
        if (active == false)
        {
            intensity_to_change_to = 0.0f;
        }
        light_change_coroutines[0] = StartCoroutine(lightsChange(0, intensity_to_change_to));
    }

    public void setEmergencyLights(bool active)
    {
        endLightChangeCoroutine(1);
        enabled_lights[1] = active;
        float intensity_to_change_to = DEFAULT_LIGHT_INTENSITIES[1];
        if (active == false)
        {
            intensity_to_change_to = 0.0f;
        }
        light_change_coroutines[1] = StartCoroutine(lightsChange(1, intensity_to_change_to));
    }

    public void enableRedAlert()
    {
        for (int i = 0; i < 2; i++)
        {
            endLightChangeCoroutine(i);
            light_intensities[i] = RED_ALERT_LIGHT_INTENSITIES[i];
            if (i == 0) //default
            {
                for (int k = 0; k < 4; k++)
                {
                    current_default_colors[k] = RED_ALERT_LIGHT_COLORS[0];
                    current_default_materials[k] = ReferenceAssistor.Instance.lit_red;
                }
            }
            else //emergency
            {
                emergency_color = RED_ALERT_LIGHT_COLORS[1];
                emergency_material = ReferenceAssistor.Instance.lit_red;
            }
            displayLightIntensities(i);
            displayLightMaterials(i);
            displayLightColors(i);
        }
    }

    public void disableRedAlert()
    {
        for (int i = 0; i < 2; i++)
        {
            endLightChangeCoroutine(i);
            light_intensities[i] = DEFAULT_LIGHT_INTENSITIES[i];
            if (i == 0) //default
            {
                for (int k = 0; k < 4; k++)
                {
                    current_default_colors[k] = normal_default_colors[k];
                    current_default_materials[k] = normal_default_materials[k];
                }
            }
            else //emergency
            {
                emergency_color = DEFAULT_LIGHT_COLORS[1];
                emergency_material = ReferenceAssistor.Instance.lit_off_white;
            }
            displayLightIntensities(i);
            displayLightMaterials(i);
            displayLightColors(i);
        }
    }

    public void flickerLights(int section, float time)
    {
        //only flicker if lights are on
        if (enabled_lights[0] == false)
        {
            return;
        }

        //check if already flickering in section, and if so, increase time (if new time is greater than current)
        if (flicker_coroutines[section] != null)
        {
            if (time > flicker_times[section])
            {
                flicker_times[section] = time;
            }
        }
        else
        {
            flicker_times[section] = time;
            flicker_coroutines[section] = StartCoroutine(sectionFlicker(section));
        }
    }

    private void adjustSectionFlicker(int section, float dim_factor)
    {
        Material section_material = current_default_materials[section];
        flicker_materials[section].SetColor("_BaseColor", section_material.GetColor("_BaseColor") * Mathf.Lerp(0.05f, 1.0f, dim_factor));
        flicker_materials[section].SetColor("_EmissionColor", section_material.GetColor("_EmissionColor") * Mathf.Lerp(0.35f, 1.0f, dim_factor));
        for (int i = 0; i < default_renderers[section].Count; i++)
        {
            default_renderers[section][i].material = flicker_materials[section];
            default_lights[section][i].intensity = Mathf.Lerp(1.0f, light_intensities[0], dim_factor);
        }
        for (int i = 0; i < lit_renderers[section].Count; i++)
        {
            lit_renderers[section][i].material = flicker_materials[section];
        }
    }

    IEnumerator sectionFlicker(int section)
    {
        float dim_factor = 0.0f;
        while (flicker_times[section] > 0.0f)
        {
            dim_factor = Random.Range(0.0f, 0.75f);
            adjustSectionFlicker(section, dim_factor);

            float delay = Random.Range(0.06f, 0.1f);
            flicker_times[section] = Mathf.Max(0.0f, flicker_times[section] - delay);

            yield return new WaitForSeconds(delay);
        }

        //reset to normal
        float reset_time = 0.5f;
        while (reset_time > 0.0f)
        {
            reset_time = Mathf.Max(0.0f, reset_time - Time.deltaTime);
            adjustSectionFlicker(section, Mathf.Lerp(1.0f, dim_factor, reset_time / 0.5f));
            yield return null;
        }

        flicker_coroutines[section] = null;

        //set to normal material
        displayLightMaterials(0);
    }
}