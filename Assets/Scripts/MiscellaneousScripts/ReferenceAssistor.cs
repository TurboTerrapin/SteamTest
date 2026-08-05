/*
    ReferenceAssistor.cs
    - Used to streamline referencing for certain commonly-used things
    Contributor(s): Jake Schott
    Last Updated: 7/31/2026
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ReferenceAssistor : MonoBehaviour
{
    //CLASS CONSTANTS 
    public static Color[] COLOR_OPTIONS = new Color[4] { new Color(0.0f, 0.84f, 1.0f), new Color(0.69f, 0.0f, 0.69f), new Color(1.0f, 0.47f, 0.0f), new Color(0.13f, 1.0f, 0.04f) }; //pilot, tactician, engineer, captain
    public static string[] STATION_NAMES = { "PILOT", "TACTICIAN", "ENGINEER", "CAPTAIN" };

    public List<Texture> position_icons = null;

    public Material lit_neon;
    public Material unlit_neon;
    public Material lit_red;
    public Material unlit_red;
    public Material lit_green;
    public Material unlit_green;
    public Material lit_purple;
    public Material unlit_purple;
    public Material lit_orange;
    public Material unlit_orange;
    public Material lit_dark_blue;
    public Material unlit_dark_blue;
    public Material lit_off_white;
    public Material lit_white;
    public Material unlit_white;
    public Material pure_black;

    public GameObject light_layer_one;
    public GameObject light_layer_two;

    public List<GameObject> module_handlers;

    public List<GameObject> collectible_items;

    public GameObject world_root;
    public GameObject spaceship;
    public Volume camera_settings;
    public AudioManager audio_manager;
    public EffectsHandler effects_handler;
    public FailureHandler failure_handler;
    public PlayerManager player_manager;
    public PowerManager power_manager;
    public ScenarioManager scenario_manager;
    public SeatManager seat_manager;

    public static ReferenceAssistor Instance { get; private set; }

    private void Awake()
    {
        //make an instance so can be referenced
        if (Instance != null)
        {
            Destroy(this);
        }
        Instance = this;
    }

    public void assignWorldRoot(GameObject wr)
    {
        world_root = wr;
    }
}