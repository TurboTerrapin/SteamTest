/*
    ShipHealth.cs
    - Handles ship health for four areas of the ship (forward, port, starboard, aft)
    - Handles hull integrity (health of the most damaged section)
    - Updates screens in engineer position
    Contributor(s): Jake Schott
    Last Updated: 3/20/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShipHealth : NetworkBehaviour, IPowerable
{
    //CLASS CONSTANTS
    private static float[] DAMAGE_MODIFIERS = new float[] { 0.5f, 0.7f, 0.85f, 1.0f }; //corresponds to easy, medium, hard, expert
    private static float UPDATE_TIME = 1.0f;
    private static Color MAX_HEALTH = new Color(0.34f, 1.0f, 0.0f, 0.21f);
    private static Color HALF_HEALTH = new Color(1.0f, 1.0f, 0.0f, 0.21f);
    private static Color ZERO_HEALTH = new Color(1.0f, 0.0f, 0.0f, 0.21f);
    private static bool INVINCIBLE_SHIP = true; //used for testing

    public GameObject hull_integrity_display;
    public GameObject ship_overview_display;
    public LightsManager lights_manager;
    public List<AudioClip> hull_creak_sounds = new List<AudioClip>();
    public AudioSource hull_creak_source;
    private PlayerManager player_manager;
    private ScenarioManager scenario_manager;
    private ShieldStrength shield_strength;

    public List<GameObject> ship_health_indicators = null;
    public GameObject hull_integrity_visual;
    public GameObject hull_integrity_percentages;

    private float[] health_areas = new float[4] { 100.0f, 100.0f, 100.0f, 100.0f }; //corresponds to forward, port, starboard, aft
    private float hull_integrity = 100.0f;
    private Coroutine damage_animation_coroutine = null;
    private Coroutine dead_ship_coroutine = null;

    private void Start()
    {
        player_manager = GameObject.FindGameObjectWithTag("PlayerManager").GetComponent<PlayerManager>();
        scenario_manager = GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>();
        shield_strength = ReferenceAssistor.Instance.module_handlers[2].GetComponent<ShieldStrength>();
    }

    public float getHullIntegrity()
    {
        return Mathf.Max(0.0f, hull_integrity);
    }

    public static Color getDesiredColor(float health)
    {
        health = Mathf.Max(0.0f, health);
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
            hull_integrity_percentages.transform.GetChild(i).GetChild(0).GetComponent<TMP_Text>().SetText(Mathf.FloorToInt(health_areas[i]).ToString() + "%");
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
            hull_integrity_percentages.transform.GetChild(4).GetChild(0).GetComponent<TMP_Text>().SetText(hull_integrity_text);

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
        scenario_manager.endScenario(ScenarioManager.EndCondition.ShipDestroyed);
    }

    //compares before and after health of a given section and does damage / 5 for flicker time in that section
    private void showDamageEffects(int section, float damage)
    {
        if (damage == 0.0f)
        {
            return;
        }

        if (damage > 0.5f)
        {
            if (hull_creak_source.isPlaying == false)
            {
                hull_creak_source.clip = hull_creak_sounds[Random.Range(0, hull_creak_sounds.Count)];
                hull_creak_source.Play();
            }
        }

        float flicker_time = damage * 0.2f;
        lights_manager.flickerLights(section, flicker_time);
    }

    //helper function that returns true if no shield battery available in section or effect inactive
    private bool attemptSectionDamage(float dam, int section)
    {
        //if shield effect active, prevent damage
        if (shield_strength.getShieldEffectTime(section) > 0.0f)
        {
            return false;
        }

        //if damage less than 1 and shield battery in section, ignore
        if (shield_strength.getShieldStrength(section) > 0 && dam < 1.0f)
        {
            return false;
        }

        //use shield battery if available
        if (shield_strength.attemptShieldUsage(section) == true)
        {
            return false;
        }

        return true;
    }

    //helper function that subtracts damage and rounds to nearest tenth
    private float updateHealth(float dam, int section)
    {
        float updated_health = Mathf.Max(0.0f, health_areas[section] - dam);
        updated_health = Mathf.Round(updated_health * 10.0f) / 10.0f;
        return updated_health;
    }

    public void damageSection(float damage, int section)
    {
        if (NetworkManager.Singleton.IsHost == false || INVINCIBLE_SHIP == true)
        {
            return;
        }
        damage *= DAMAGE_MODIFIERS[scenario_manager.getDifficulty()];

        //attempt to use shield battery or check if effect currently active
        if (attemptSectionDamage(damage, section) == true)
        {
            //no shield protection, damage section
            updateHealth(damage, section);
            float[] temp_health_areas = new float[4];
            for (int i = 0; i < 4; i++)
            {
                temp_health_areas[i] = health_areas[i];
            }
            temp_health_areas[section] = updateHealth(damage, section);
            transmitHealthChangeRPC(temp_health_areas[0], temp_health_areas[1], temp_health_areas[2], temp_health_areas[3]);
        }
        transmitDamageAttemptRPC(damage);
    }

    //will damage every section randomly between 0.0 and full damage but ensure that one is damaged as much as inputted parameter
    public void damageAllSections(float damage)
    {
        if (NetworkManager.Singleton.IsHost == false || INVINCIBLE_SHIP == true)
        {
            return;
        }

        damage *= DAMAGE_MODIFIERS[scenario_manager.getDifficulty()];
        float[] temp_health_areas = new float[4];
        for (int i = 0; i < 4; i++)
        {
            temp_health_areas[i] = health_areas[i];
        }
        int most_damaged_area = Random.Range(0, 4);
        float most_damage = damage;
        if (attemptSectionDamage(damage, most_damaged_area) == true)
        {
            temp_health_areas[most_damaged_area] = updateHealth(damage, most_damaged_area);
        }
        for (int i = 0; i < 4; i++)
        {
            float attempted_damage = Random.Range(0.0f, damage);
            if (attemptSectionDamage(attempted_damage, i) == true && i != most_damaged_area)
            {
                temp_health_areas[i] = updateHealth(attempted_damage, i);
            }
        }
        transmitDamageAttemptRPC(most_damage);
        transmitHealthChangeRPC(temp_health_areas[0], temp_health_areas[1], temp_health_areas[2], temp_health_areas[3]);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDamageAttemptRPC(float damage)
    {
        player_manager.getLocalPlayer().GetComponent<CameraMove>().ShakeCamera(Mathf.Max(1.5f, damage * 0.35f), damage * 0.2f);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitHealthChangeRPC(float fwd_health, float port_health, float stbd_health, float aft_health)
    {
        //compare areas for flicker effect
        float[] damages = new float[4] { health_areas[0] - fwd_health, health_areas[1] - port_health, health_areas[2] - stbd_health, health_areas[3] - aft_health };
        for (int i = 0; i < 4; i++)
        {
            showDamageEffects(i, damages[i]);
        }

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
        hull_integrity = Mathf.Max(0.0f, lowest_health);
        if (damage_animation_coroutine != null)
        {
            StopCoroutine(damage_animation_coroutine);
        }
        damage_animation_coroutine = StartCoroutine(showDamageEffects(prev_hull_integrity));
    }

    public void powerOn(int position)
    {
        if (hull_integrity_display.activeSelf == false)
        {
            hull_integrity_display.SetActive(true);
            return;
        }
        ship_overview_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        hull_integrity_display.SetActive(false);
        ship_overview_display.SetActive(false);
    }
}