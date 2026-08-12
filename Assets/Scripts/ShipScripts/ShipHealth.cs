/*
    ShipHealth.cs
    - Handles ship health for four areas of the ship (forward, port, starboard, aft)
    - Handles hull integrity (health of the most damaged section)
    - Updates screens in engineer position
    Contributor(s): Jake Schott
    Last Updated: 6/13/2026
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
    private static float UPDATE_SPEED = 5.0f; //how long it takes to display health changes on engineer screen
    private static Color MAX_HEALTH = new Color(0.0f, 0.84f, 1.0f, 0.2f);
    private static Color HALF_HEALTH = new Color(0.68f, 0.35f, 0.3f, 0.2f);
    private static Color ZERO_HEALTH = new Color(1.0f, 0.0f, 0.0f, 0.2f);
    private static bool INVINCIBLE_SHIP = true; //used for testing

    public GameObject hull_integrity_display;
    public GameObject ship_overview_display;
    public LightsManager lights_manager;
    public List<AudioClip> hull_creak_sounds = null;
    public List<AudioClip> hull_integrity_notifications = null;
    public AudioSource hull_creak_source;
    private PlayerManager player_manager;
    private ScenarioManager scenario_manager;
    private ShieldStrength shield_strength;

    public List<GameObject> ship_health_indicators = null;
    public GameObject hull_integrity_visual_sections_display;
    public GameObject hull_integrity_section_percentages_display;

    private float[] actual_section_integrities = new float[4] { 100.0f, 100.0f, 100.0f, 100.0f }; //corresponds to forward, port, starboard, aft
    private float[] displayed_section_integrities = new float[4] { 100.0f, 100.0f, 100.0f, 100.0f }; //corresponds to forward, port, starboard, aft
    private float hull_integrity = 100.0f;
    private Coroutine health_change_coroutine = null;
    private Coroutine dead_ship_coroutine = null;

    private void Start()
    {
        player_manager = ReferenceAssistor.Instance.player_manager;
        scenario_manager = ReferenceAssistor.Instance.scenario_manager;
        shield_strength = ReferenceAssistor.Instance.module_handlers[2].GetComponent<ShieldStrength>();
    }

    public float getHullIntegrity()
    {
        return Mathf.Max(0.0f, hull_integrity);
    }

    public static Color getHealthColor(float health)
    {
        health = Mathf.Max(0.0f, health);
        Color desired_color = new Color();
        if (health > 50.0)
        {
            desired_color = Color.Lerp(HALF_HEALTH, MAX_HEALTH, (health - 50.0f) / 50.0f);
        }
        else
        {
            desired_color = Color.Lerp(ZERO_HEALTH, HALF_HEALTH, health / 50.0f);
        }
        return desired_color;
    }

    //helper method for health change loop
    private bool displayedHealthEqualsActualHealth()
    {
        for (int i = 0; i < 4; i++)
        {
            if (actual_section_integrities[i] != displayed_section_integrities[i])
            {
                return false;
            }
        }
        return true;
    }

    //updates until matches actual integrities
    IEnumerator displayHealthChangeUpdates()
    {
        Color c;
        int weakest_section = -1;
        float weakest_health;
        while (displayedHealthEqualsActualHealth() == false)
        {
            int current_weakest_section = -1;
            weakest_health = 100.0f;

            //update health for each section
            for (int i = 0; i < 4; i++)
            {
                //move towards actual
                displayed_section_integrities[i] = Mathf.MoveTowards(displayed_section_integrities[i], actual_section_integrities[i], Time.deltaTime * UPDATE_SPEED);

                //check if weakest section
                if (displayed_section_integrities[i] < weakest_health)
                {
                    current_weakest_section = i;
                    weakest_health = displayed_section_integrities[i];
                }

                //check if need to update and if so, update
                if (actual_section_integrities[i] != displayed_section_integrities[i])
                {
                    hull_integrity_section_percentages_display.transform.GetChild(i).GetComponent<TMP_Text>().SetText(Mathf.FloorToInt(displayed_section_integrities[i]).ToString() + "%");
                    hull_integrity_section_percentages_display.transform.GetChild(i).GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = displayed_section_integrities[i] / 100.0f;
                    c = getHealthColor(displayed_section_integrities[i]);
                    c.a = 0.2f;
                    ship_health_indicators[i].GetComponent<UnityEngine.UI.RawImage>().color = c;
                    hull_integrity_section_percentages_display.transform.GetChild(i).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = c;
                    c.a = 0.08f;
                    hull_integrity_section_percentages_display.transform.GetChild(i).GetChild(3).GetComponent<UnityEngine.UI.RawImage>().color = c;
                }
            }

            //highlight weakest section
            if (weakest_section != current_weakest_section)
            {
                weakest_section = current_weakest_section;
                for (int i = 0; i < 4; i++)
                {
                    hull_integrity_section_percentages_display.transform.GetChild(i).GetComponent<TMP_Text>().fontSize = 0.035f;
                }

                //if there is a weakest section, move arrows and increase font for weakest section
                hull_integrity_section_percentages_display.transform.GetChild(4).gameObject.SetActive(weakest_section >= 0);
                if (weakest_section >= 0)
                {
                    hull_integrity_section_percentages_display.transform.GetChild(weakest_section).GetComponent<TMP_Text>().fontSize = 0.04f;
                    hull_integrity_section_percentages_display.transform.GetChild(4).localPosition = new Vector3(0.0f, hull_integrity_section_percentages_display.transform.GetChild(weakest_section).localPosition.y, 0.0f);
                }
            }

            yield return null;
        }

        health_change_coroutine = null;
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
        if (damage <= 0.0f)
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
        float updated_health = Mathf.Max(0.0f, actual_section_integrities[section] - dam);
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
                temp_health_areas[i] = actual_section_integrities[i];
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
            temp_health_areas[i] = actual_section_integrities[i];
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
        float[] damages = new float[4] { actual_section_integrities[0] - fwd_health, actual_section_integrities[1] - port_health, actual_section_integrities[2] - stbd_health, actual_section_integrities[3] - aft_health };
        for (int i = 0; i < 4; i++)
        {
            showDamageEffects(i, damages[i]);
        }

        //set section integrities
        actual_section_integrities[0] = fwd_health;
        actual_section_integrities[1] = port_health;
        actual_section_integrities[2] = stbd_health;
        actual_section_integrities[3] = aft_health;

        //set hull integrity to whichever is lowest
        float lowest_health = 100.0f;
        int weakest_section = -1;
        for (int i = 0; i < 4; i++)
        {
            if (actual_section_integrities[i] < lowest_health)
            {
                lowest_health = actual_section_integrities[i];
                weakest_section = i;
            }
        }
        float prev_hull_integrity = hull_integrity;
        hull_integrity = Mathf.Max(0.0f, lowest_health);

        //play notification sound if threshold crossed
        float[] thresholds = new float[4] { 75.0f, 50.0f, 25.0f, 10.0f };
        for (int i = 0; i < 4; i++)
        {
            if (prev_hull_integrity > thresholds[i] && hull_integrity <= thresholds[i])
            {
                ReferenceAssistor.Instance.audio_manager.AddNotification(1, hull_integrity_notifications[i]);
                break;
            }
        }

        //kill ship if hull integrity at 0
        if (NetworkManager.Singleton.IsHost == true && hull_integrity <= 0.0f)
        {
            if (dead_ship_coroutine == null)
            {
                dead_ship_coroutine = StartCoroutine(deadDelay());
            }
        }

        //display on engineer health screen
        if (health_change_coroutine == null)
        {
            health_change_coroutine = StartCoroutine(displayHealthChangeUpdates());
        }
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