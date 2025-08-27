/*
    ShipHealth.cs
    - Handles ship health for four areas of the ship (forward, port, starboard, aft)
    - Handles hull integrity (health of the most damaged section)
    - Updates screens in engineer position
    Contributor(s): Jake Schott
    Last Updated: 6/8/2025
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShipHealth : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float UPDATE_TIME = 1.0f;
    private static Color MAX_HEALTH = new Color(0.34f, 1.0f, 0.0f, 0.21f);
    private static Color HALF_HEALTH = new Color(1.0f, 1.0f, 0.0f, 0.21f);
    private static Color ZERO_HEALTH = new Color(1.0f, 0.0f, 0.0f, 0.21f);

    public List<GameObject> ship_health_indicators = null;
    public GameObject hull_integrity_visual;
    public GameObject hull_integrity_percent_text;

    private float[] health_areas = new float[4] { 100.0f, 100.0f, 100.0f, 100.0f }; //corresponds to forward, port, starboard, aft
    private float hull_integrity = 100.0f;
    private Coroutine damage_animation_coroutine = null;
    private Coroutine dead_ship_coroutine = null;

    /*private void Start()
    {
        StartCoroutine(tester());
    }

    IEnumerator tester()
    {
        yield return new WaitForSeconds(5.0f);
        while (true)
        {
            damageAllSections(6.5f);
            yield return new WaitForSeconds(2.5f);
        }
    }*/

    public float getHullIntegrity()
    {
        return hull_integrity;
    }

    public static Color getDesiredColor(float health)
    {
        Color desired_color = new Color();
        if (health > 50.0)
        {
            desired_color =
                new Color(Mathf.Lerp(HALF_HEALTH.r, MAX_HEALTH.r, (health - 50.0f) / 50.0f),
                          Mathf.Lerp(HALF_HEALTH.g, MAX_HEALTH.g, (health - 50.0f) / 50.0f),
                          Mathf.Lerp(HALF_HEALTH.b, MAX_HEALTH.b, (health - 50.0f) / 50.0f),
                          Mathf.Lerp(HALF_HEALTH.a, MAX_HEALTH.a, (health - 50.0f) / 50.0f));
        }
        else
        {
            desired_color =
                new Color(Mathf.Lerp(ZERO_HEALTH.r, HALF_HEALTH.r, health / 50.0f),
                          Mathf.Lerp(ZERO_HEALTH.g, HALF_HEALTH.g, health / 50.0f),
                          Mathf.Lerp(ZERO_HEALTH.b, HALF_HEALTH.b, health / 50.0f),
                          Mathf.Lerp(ZERO_HEALTH.a, HALF_HEALTH.a, health / 50.0f));
        }
        return desired_color;
    }

    //helper function used to set the colors of the different damage areas of the ship
    private void setColorHelper(GameObject image, Color start, Color end, float percent_to_end)
    {
        image.GetComponent<UnityEngine.UI.RawImage>().color =
            new Color(Mathf.Lerp(end.r, start.r, percent_to_end),
                      Mathf.Lerp(end.g, start.g, percent_to_end),
                      Mathf.Lerp(end.b, start.b, percent_to_end),
                      Mathf.Lerp(end.a, start.a, percent_to_end));
    }

    IEnumerator showDamageEffects(float prev_hull_integrity)
    {
        Color[] start_colors = new Color[5];
        Color[] desired_colors = new Color[5];
        for (int i = 0; i < 4; i++)
        {
            start_colors[i] = ship_health_indicators[i].GetComponent<UnityEngine.UI.RawImage>().color;
            desired_colors[i] = getDesiredColor(health_areas[i]);
        }
        start_colors[4] = hull_integrity_visual.GetComponent<UnityEngine.UI.RawImage>().color;
        desired_colors[4] = getDesiredColor(hull_integrity);

        float animation_time = UPDATE_TIME;

        while (animation_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            animation_time = Mathf.Max(0.0f, animation_time - dt);

            //adjust four sections
            for (int i = 0; i < 4; i++)
            {
                setColorHelper(ship_health_indicators[i], start_colors[i], desired_colors[i], animation_time / UPDATE_TIME);
            }

            //adjust hull integrity
            setColorHelper(hull_integrity_visual, start_colors[4], desired_colors[4], animation_time / UPDATE_TIME);
            string hull_integrity_text = ((Mathf.Round(Mathf.Lerp(prev_hull_integrity, hull_integrity, 1.0f - (animation_time / UPDATE_TIME)) * 10.0f)) / 10.0f).ToString();
            if (hull_integrity_text.Contains(".") == false)
            {
                hull_integrity_text += ".0";
            }
            hull_integrity_text += "%";
            hull_integrity_percent_text.GetComponent<TMP_Text>().SetText(hull_integrity_text);

            yield return null;
        }

        if (hull_integrity <= 0.0f)
        {
            if (dead_ship_coroutine == null)
            {
                dead_ship_coroutine = StartCoroutine(deadDelay());
            }

        }

        damage_animation_coroutine = null;
    }

    //just used to give a little bit of a wait before cutting to game over screen
    IEnumerator deadDelay()
    {
        yield return new WaitForSeconds(2.0f);
        GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>().endScenario(ScenarioManager.EndCondition.ShipDestroyed);
    }

    //helper function that subtracts damage and rounds to nearest tenth
    private void updateHealth(float dam, int index)
    {
        float updated_health = Mathf.Max(0.0f, health_areas[index] - dam);
        updated_health = Mathf.Round(updated_health * 10.0f) / 10.0f;
        health_areas[index] = updated_health;
    }

    public void damageSection(float damage, int section)
    {
        updateHealth(damage, section);
        transmitHealthChangeRPC(health_areas[0], health_areas[1], health_areas[2], health_areas[3]);
    }

    //will damage every section randomly between 0.0 and full damage but ensure that one is damaged as much as inputted parameter
    public void damageAllSections(float damage)
    {
        int most_damaged_area = Random.Range(0, 4);
        updateHealth(damage, most_damaged_area);
        for (int i = 0; i < 4; i++)
        {
            if (i != most_damaged_area)
            {
                updateHealth(Random.Range(0.0f, damage), i);
            }
        }
        transmitHealthChangeRPC(health_areas[0], health_areas[1], health_areas[2], health_areas[3]);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitHealthChangeRPC(float fwd_health, float port_health, float stbd_health, float aft_health)
    {
        //set areas
        health_areas[0] = fwd_health;
        health_areas[1] = port_health;
        health_areas[2] = stbd_health;
        health_areas[3] = aft_health;

        //set hull integrity to whichever is lowest
        float lowest_health = 9999.9f;
        int lowest_area = -1;
        for (int i = 0; i < 4; i++)
        {
            if (health_areas[i] < lowest_health)
            {
                lowest_health = health_areas[i];
                lowest_area = i;
            }
        }
        float prev_hull_integrity = hull_integrity;
        hull_integrity = lowest_health;
        if (damage_animation_coroutine != null)
        {
            StopCoroutine(damage_animation_coroutine);
        }
        damage_animation_coroutine = StartCoroutine(showDamageEffects(prev_hull_integrity));
    }
}
