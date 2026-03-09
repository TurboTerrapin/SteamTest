/*
    LightsManager.cs
    - Handles light stuff
    Contributor(s): Jake Schott, Henryk Musial
    Last Updated: 2/15/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightsManager : MonoBehaviour
{
    //CLASS CONSTANTS (0 IS DEFAULT, 1 IS EMERGENCY LIGHTS)
    private static float[] LIGHT_CHANGE_TIME = new float[2] { 0.5f, 0.5f };
    private static float[] DEFAULT_LIGHT_INTENSITY = new float[2] { 15.0f, 10.0f };
    private static Color[] DEFAULT_LIGHT_COLOR = new Color[] { new Color(0.66f, 0.92f, 1.0f), new Color(0.87f, 0.96f, 1.0f) };
    private static Material[] DEFAULT_LIGHT_MATERIAL = new Material[2] { null, null };
    private static float[] RED_ALERT_LIGHT_INTENSITY = new float[2] { 10.0f, 10.0f };
    private static Color[] RED_ALERT_LIGHT_COLOR = new Color[] { new Color(1.0f, 0.0f, 0.0f), new Color(0.8f, 0.02f, 0.0f) };

    public Material lit_neon;
    public Material unlit_neon;
    public Material lit_off_white;
    public Material lit_red;
    public Material pure_black;

    public GameObject default_lights;
    public GameObject emergency_lights;
    public GameObject light_strips;
    public GameObject energy_circles;

    private GameObject[] light_groups = new GameObject[2] { null, null };
    private ShipStatus ship_status;

    private bool[] enabled_lights = new bool[2] { true, false };
    private Coroutine[] light_change_coroutines = new Coroutine[2] { null, null };

    // Added - Henryk
    private Coroutine flicker_coroutine = null;
    private static float BASE_FLICKER_DURATION = 3.0f;
    private static float FLICKER_DAMAGE_THRESHOLD = 15.0f;
    private List<Material> tempMaterials = new List<Material>(); // Stores the instanced dimmed materials for flickering

    [Range(0.0f, 1.0f)]
    private float minDimnessAtMaxDamage = 0.01f;
    private float minFlickerOffDuration = 0.05f;
    private float maxFlickerOffDuration = 0.1f;
    private float minFlickerOnDuration = 0.025f;
    private float maxFlickerOnDuration = 0.3f;

    private void Start()
    {
        light_groups[0] = default_lights;
        light_groups[1] = emergency_lights;

        DEFAULT_LIGHT_MATERIAL[0] = lit_neon;
        DEFAULT_LIGHT_MATERIAL[1] = lit_off_white;

        ship_status = ReferenceAssistor.Instance.module_handlers[3].GetComponent<ShipStatus>();
    }


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

        if (flicker_coroutine != null)
        {
            StopCoroutine(flicker_coroutine);
            flicker_coroutine = null;
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

        //enable light strips
        changeLightStrips(lit_neon);
        changeEnergyCircles(lit_neon);
    }

    //helper method that changes every light strip's material
    private void changeLightStrips(Material to_change_to)
    {
        foreach (Transform strip in light_strips.transform)
        {
            strip.GetComponent<Renderer>().material = to_change_to;
        }
    }

    //helper method that changes every energy circle's material
    private void changeEnergyCircles(Material to_change_to)
    {
        foreach (Transform strip in energy_circles.transform)
        {
            strip.GetComponent<Renderer>().material = to_change_to;
        }
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

        //enable light strips
        changeLightStrips(lit_neon);
        changeEnergyCircles(lit_neon);
    }  

    public void disableDefaultLights()
    {
        resetLightChangeCoroutine(0);
        enabled_lights[0] = false;
        light_change_coroutines[0] = StartCoroutine(lightsChange(0, 0.0f));

        //disable light strips
        changeLightStrips(pure_black);
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

        //change light strip color
        if (enabled_lights[0] == true)
        {
            changeLightStrips(lit_red);
            changeEnergyCircles(lit_red);
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

        //change light strip color
        if (enabled_lights[0] == true)
        {
            changeLightStrips(lit_neon);
            changeEnergyCircles(lit_neon);
        }
    }

    public void TriggerCollisionFlicker(float collisionDamage)
    {
        if (collisionDamage < FLICKER_DAMAGE_THRESHOLD) return;

        // Stop any ongoing light changes
        for (int i = 0; i < light_change_coroutines.Length; i++)
        {
            if (light_change_coroutines[i] != null)
            {
                StopCoroutine(light_change_coroutines[i]);
                light_change_coroutines[i] = null;
            }
        }

        // Restart flicker effect
        if (flicker_coroutine != null)
        {
            StopCoroutine(flicker_coroutine);
        }
        flicker_coroutine = StartCoroutine(FlickerEffectCoroutine(collisionDamage));
    }

    // Flickers both the lights and the emissive materials (instanced)
    private IEnumerator FlickerEffectCoroutine(float damage)
    {
        // Calculate the flicker parameters
        float normalized_damage = Mathf.Clamp01(damage / 100.0f);
        float flicker_duration = BASE_FLICKER_DURATION * normalized_damage;
        float dim_factor = Mathf.Lerp(1.0f, minDimnessAtMaxDamage, normalized_damage);

        // Cache all renderers and create dimmed materials once
        List<Renderer> activeRenderers = new List<Renderer>();
        List<Material> dimmedMaterials = new List<Material>();
        List<Material> tempMaterials = new List<Material>();
        bool is_red_alert = ship_status.getCurrColor() == 2;

        for (int i = 0; i < 2; i++)
        {
            if (!enabled_lights[i]) continue;

            Transform light_group = light_groups[i].transform;
            foreach (Transform light_transform in light_group)
            {
                foreach (Transform physical_light in light_transform)
                {
                    Renderer rend = physical_light.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        activeRenderers.Add(rend);

                        // Create a dimmed version of the current material
                        Material dimmedMat = new Material(rend.material);
                        if (dimmedMat.HasProperty("_EmissionColor"))
                        {
                            Color emissionColor = dimmedMat.GetColor("_EmissionColor");
                            dimmedMat.SetColor("_EmissionColor", emissionColor * dim_factor);
                        }
                        if (dimmedMat.HasProperty("_Color"))
                        {
                            Color color = dimmedMat.color;
                            dimmedMat.SetColor("_Color", color * dim_factor);
                        }
                        dimmedMaterials.Add(dimmedMat);
                        tempMaterials.Add(dimmedMat); // Track for cleanup
                    }
                }
            }
        }

        float elapsed_time = 0f;

        while (elapsed_time < flicker_duration)
        {
            // Dim phase - both lights and materials
            SetAllLightsIntensity(dim_factor);
            for (int i = 0; i < activeRenderers.Count; i++)
            {
                activeRenderers[i].material = dimmedMaterials[i];
            }

            float off_time = Random.Range(minFlickerOffDuration, maxFlickerOffDuration);
            yield return new WaitForSeconds(off_time);
            elapsed_time += off_time;

            if (elapsed_time >= flicker_duration) break;

            // Normal phase - restores everything
            RestoreLightsToNormal(is_red_alert);

            float on_time = Random.Range(minFlickerOnDuration, maxFlickerOnDuration);
            yield return new WaitForSeconds(on_time);
            elapsed_time += on_time;
        }

        // Restore to final state
        RestoreLightsToNormal(ship_status.getCurrColor() == 2);

        // Cleanup 
        foreach (Material mat in tempMaterials)
        {
            Destroy(mat);
        }
        tempMaterials.Clear();

        flicker_coroutine = null;
    }

    private void SetAllLightsIntensity(float multiplier)
    {
        for (int i = 0; i < 2; i++)
        {
            if (!enabled_lights[i]) continue;

            Transform light_group = light_groups[i].transform;
            float target_intensity = (ship_status.getCurrColor() == 2 ? RED_ALERT_LIGHT_INTENSITY[i] : DEFAULT_LIGHT_INTENSITY[i]) * multiplier;

            foreach (Transform light_transform in light_group)
            {
                Light light_component = light_transform.GetComponent<Light>();
                if (light_component != null)
                {
                    light_component.intensity = target_intensity;
                }
            }
        }
    }

    private void RestoreLightsToNormal(bool is_red_alert)
    {
        for (int i = 0; i < 2; i++)
        {
            if (!enabled_lights[i]) continue;

            Transform light_group = light_groups[i].transform;

            // Restore intensity
            float target_intensity = is_red_alert ? RED_ALERT_LIGHT_INTENSITY[i] : DEFAULT_LIGHT_INTENSITY[i];
            changeLightIntensities(light_group, target_intensity);

            // Restore materials
            Material target_material = is_red_alert ? lit_red : DEFAULT_LIGHT_MATERIAL[i];
            changeMaterials(light_group, target_material);
        }
    }
}
