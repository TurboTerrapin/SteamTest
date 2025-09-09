/*
    PatternVisualizer.cs
    - Handles visuals for an energy pattern as seen in the engineer position
    - Currently operates under the assumption that there are only four rings
    Contributor(s): Jake Schott
    Last Updated: 8/16/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternVisualizer : MonoBehaviour
{
    //CLASS CONSTANTS
    private static int NUM_OF_ITEMS_PER_RING = 36;

    public PatternData pattern_information;
    public GameObject pattern_center;
    public List<GameObject> rings = null; //a-d
    private Coroutine default_rotation_coroutine = null;
    private Coroutine size_change_coroutine = null;
    private Coroutine color_change_coroutine = null;

    private float ring_min_size = 0.05f;
    private float ring_separation_distance = 0.01f;

    private void patternSizeHelper(float ring_size)
    {
        //change ring size
        for (int r = 0; r < rings.Count; r++)
        {
            float ring_at_smallest = ring_min_size + (ring_separation_distance * (rings.Count - 1 - r)); //smallest size per each ring
            float ring_diamater_increase = (ring_size * (ring_min_size + (ring_separation_distance * (rings.Count - 1 - r)))) * 0.75f;

            float ring_diameter = ring_at_smallest + ring_diamater_increase;

            rings[r].GetComponent<RectTransform>().sizeDelta = new Vector2(ring_diameter, ring_diameter);
            for (int c = 0; c < rings[r].transform.GetChild(0).childCount; c++)
            {
                rings[r].transform.GetChild(0).GetChild(c).GetChild(0).localPosition =
                    new Vector3((ring_diamater_increase * -0.5f) - 0.039f + (r * 0.005f),
                                0.0f,
                                0.0f);
            }
            rings[r].transform.GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(ring_diameter - 0.005f, ring_diameter - 0.005f);
        }
    }

    //handles color changes in (runs for anim_time)
    IEnumerator patternColorChange(List<Color> end_colors, float anim_time)
    {
        Color[] starting_colors = new Color[pattern_information.getNumberOfRings()];
        for (int i = 0; i < starting_colors.Length; i++)
        {
            starting_colors[i] = pattern_information.getRingColors()[i];
        }

        List<bool> is_solid = pattern_information.getRingSolids();

        float time_remaining = anim_time;
        while (time_remaining > 0.0f)
        {
            time_remaining -= Time.deltaTime;

            for (int r = 0; r < starting_colors.Length; r++)
            {
                Color temp_color =
                    new Color(Mathf.Lerp(starting_colors[r].r, end_colors[r].r, 1.0f - (time_remaining / anim_time)),
                              Mathf.Lerp(starting_colors[r].g, end_colors[r].g, 1.0f - (time_remaining / anim_time)),
                              Mathf.Lerp(starting_colors[r].b, end_colors[r].b, 1.0f - (time_remaining / anim_time)),
                              1.0f);

                if (is_solid[r] == false) //must recolor each individual dot
                {
                    for (int x = 1; x < rings[r].transform.GetChild(0).childCount; x++)
                    {
                        rings[r].transform.GetChild(0).GetChild(x).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = temp_color;
                    }
                }
                else //recolor the ring
                {
                    rings[r].GetComponent<UnityEngine.UI.RawImage>().color = temp_color;
                }
            }

            yield return null;
        }

        pattern_information.setRingColors(end_colors);
    }

    //ringa resize to small if true, big is false (runs for time_interval) 
    IEnumerator patternSizeChange(bool contract, float time_interval)
    {
        float ring_start_size = 1.0f;
        float ring_end_size = 0.0f;
        if (contract == false) //rings are increasing in size
        {
            ring_start_size = 0.0f;
            ring_end_size = 1.0f;
        }
        float time_remaining = time_interval;
        while (time_remaining > 0.0f)
        {
            time_remaining -= Time.deltaTime;

            float ring_size = Mathf.Lerp(ring_start_size, ring_end_size, 1.0f - (time_remaining / time_interval));

            patternSizeHelper(ring_size);

            yield return null;
        }
    }

    //ring spin coroutine (runs forever)
    IEnumerator spinRings(float[] rotate_speeds)
    {
        while (true)
        {
            pattern_center.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, pattern_center.transform.localEulerAngles.z + Time.deltaTime * rotate_speeds[0]);
            for (int r = 1; r < rotate_speeds.Length; r++)
            {
                rings[r - 1].transform.localRotation = Quaternion.Euler(0.0f, 0.0f, rings[r - 1].transform.localEulerAngles.z + Time.deltaTime * rotate_speeds[r]);
            }
            yield return null;
        }
    }

    public void resetPattern()
    {
        //destroy all individual items
        for (int r = 0; r < rings.Count; r++)
        {
            for (int x = rings[r].transform.GetChild(0).childCount - 1; x >= 1; x--)
            {
                GameObject.Destroy(rings[r].transform.GetChild(0).GetChild(x).gameObject);
            }
        }

        //stop spinning
        if (default_rotation_coroutine != null)
        {
            StopCoroutine(default_rotation_coroutine);
        }
        default_rotation_coroutine = null;
    }

    public void displayPattern(PatternData pattern_info)
    {
        //set center
        pattern_center.GetComponent<UnityEngine.UI.RawImage>().texture = pattern_info.getCenterTexture();
        pattern_center.GetComponent<UnityEngine.UI.RawImage>().color = pattern_info.getCenterColor();

        //set rings
        for (int r = 0; r < pattern_info.getNumberOfRings(); r++)
        {
            if (pattern_info.getRingSolids()[r] == true) //is just a texture that spins around
            {
                rings[r].GetComponent<UnityEngine.UI.RawImage>().texture = pattern_info.getRingTextures()[r];
                rings[r].GetComponent<UnityEngine.UI.RawImage>().color = pattern_info.getRingColors()[r];
            }
            else //is comprised of more than one textures (ex. circles, diamonds)
            {
                //hide the outer ring
                rings[r].GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.0f, 0.0f, 0.0f);

                //create ring items
                for (int i = 0; i < NUM_OF_ITEMS_PER_RING; i++)
                {
                    GameObject new_item = Object.Instantiate(rings[r].transform.GetChild(0).GetChild(0).gameObject, rings[r].transform.GetChild(0));
                    new_item.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().texture = pattern_info.getRingTextures()[r];
                    new_item.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = pattern_info.getRingColors()[r];
                    new_item.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, i * (360.0f / NUM_OF_ITEMS_PER_RING));
                    new_item.SetActive(true);
                }
            }
        }

        float[] rotate_speeds = new float[pattern_info.getNumberOfRings() + 1];
        rotate_speeds[0] = pattern_info.getCenterSpeed();
        for (int r = 0; r < pattern_info.getNumberOfRings(); r++)
        {
            rotate_speeds[r + 1] = pattern_info.getRingSpeeds()[r];
        }

        pattern_information = pattern_info;

        //start at biggest
        patternSizeHelper(1.0f);

        default_rotation_coroutine = StartCoroutine(spinRings(rotate_speeds));
    }

    public void changeColors(List<Color> new_colors, float anim_time)
    {
        if (color_change_coroutine != null)
        {
            StopCoroutine(color_change_coroutine);
        }

        color_change_coroutine = StartCoroutine(patternColorChange(new_colors, anim_time));
    }

    public void contractPattern(float time_interval)
    {
        if (size_change_coroutine != null)
        {
            StopCoroutine(size_change_coroutine);
        }
        size_change_coroutine = StartCoroutine(patternSizeChange(true, time_interval));
    }

    public void expandPattern(float time_interval)
    {
        if (size_change_coroutine != null)
        {
            StopCoroutine(size_change_coroutine);
        }
        size_change_coroutine = StartCoroutine(patternSizeChange(false, time_interval));
    }
}