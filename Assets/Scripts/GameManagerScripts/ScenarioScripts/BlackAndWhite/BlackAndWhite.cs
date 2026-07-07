/*
    BlackAndWhite.cs
    - Handles all the functions pertaining to the black-and-white scenario (the wall one)
    Contributor(s): Jake Schott
    Last Updated: 7/6/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class BlackAndWhite : NetworkBehaviour, IScenario, IComputerRegulatorSusceptible
{
    //CLASS CONSTANTS
    private static string DEATH_MESSAGE = "Stolen ship SEACC-3002 was found destroyed near an anomalous barrier. Crew was unable to disable the wall without sustaining critical damage. No survivors were found.";
    private static Color[] LIGHT_BEAM_COLORS_OPTIONS = new Color[9] { ReferenceAssistor.COLOR_OPTIONS[0], ReferenceAssistor.COLOR_OPTIONS[1], ReferenceAssistor.COLOR_OPTIONS[2], ReferenceAssistor.COLOR_OPTIONS[3], Color.white, Color.red, Color.yellow, Color.blue, new Color(1.0f, 0.0f, 0.5f) };
    private static float COLOR_RESTORATION_TIME = 5.0f;
    private static int[] NUM_NODES_TO_RESTORE_COLOR = new int[] { 1, 2, 3, 4 }; //corresponds to easy, medium, hard, expert difficulties

    public AudioSource stun_sound;
    public List<Material> light_beam_material_options = null;  
    public List<Mesh> radiation_options = null;
    public List<GameObject> radiation_emitters = null;
    public List<GameObject> shield_generators = null;
    public List<GameObject> shields = null;
    public GameObject vertical_light_beams;
    public GameObject horizontal_light_beams;
    public GameObject extended_light_beams;
    public BWWall ship_barrier;

    private bool color_restored = false;
    private int special_emitter = -1; //the emitter that must be destroyed to bring color back
    private int[] emitter_radiation_patterns = new int[6] { -1, -1, -1, -1, -1, -1 }; //corresponds to A, B, C, D, E, and F mesh options
    private bool[] emitter_active_states = new bool[6] { true, true, true, true, true, true };
    private List<int> intersect_vertical_colors = new List<int>();
    private int intersect_horizontal_color;
    private int[] vertical_light_colors = new int[9] { -1, -1,-1, -1, -1, -1, -1, -1, -1 };
    private int[] horizontal_light_colors = new int[7] { -1, -1, -1, -1, -1, -1, -1 };
    private UnityEngine.Rendering.Universal.ColorAdjustments color_adjustments;

    private void Start()
    {
        //set screen to black-and-white
        ReferenceAssistor.Instance.camera_settings.GetComponent<Volume>().profile.TryGet(out color_adjustments);
        color_adjustments.active = true;
        color_adjustments.saturation.Override(-100.0f);

        //enable light layer two
        ReferenceAssistor.Instance.light_layer_two.gameObject.SetActive(true);
    }

    //only run by host
    public void prepScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }
    }

    //only run by host
    public void initiateScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        //set radiation program to false
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<ComputerRegulator>().overrideProgramActiveState(0, 2, false);

        //activate pushback effect of wall
        ship_barrier.activate();

        //randomize order of radiation patterns on emitters
        List<int> remaining_options = new List<int>() { 0, 1, 2, 3, 4, 5 };
        for (int i = 0; i < 6; i++)
        {
            int current_option = remaining_options[Random.Range(0, remaining_options.Count)];
            remaining_options.Remove(current_option);
            emitter_radiation_patterns[i] = current_option;
        }

        //randomize which radiation emitter needs to be destroyed to bring back color
        special_emitter = Random.Range(0, 6);

        //randomize vertical light beams
        remaining_options = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        for (int i = 0; i < 9; i++)
        {
            int current_option = remaining_options[Random.Range(0, remaining_options.Count)];
            remaining_options.Remove(current_option);
            vertical_light_colors[i] = current_option;
        }

        //randomize horizontal light beams
        remaining_options = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        for (int i = 0; i < 7; i++)
        {
            int current_option = remaining_options[Random.Range(0, remaining_options.Count)];
            remaining_options.Remove(current_option);
            horizontal_light_colors[i] = current_option;
        }

        //determine intersect colors
        remaining_options = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        for (int i = 0; i < NUM_NODES_TO_RESTORE_COLOR[ReferenceAssistor.Instance.scenario_manager.getDifficulty()]; i++)
        {
            int current_option = remaining_options[Random.Range(0, remaining_options.Count)];
            remaining_options.Remove(current_option);
            intersect_vertical_colors.Add(current_option);
        }
        intersect_horizontal_color = horizontal_light_colors[Random.Range(0, 7)];

        //change spatial composition analyzer
        List<int> sca_quantities = new List<int>();
        List<int> sca_molecules = new List<int>();
        List<int> sca_colors = new List<int>();
        for (int i = 0; i < NUM_NODES_TO_RESTORE_COLOR[ReferenceAssistor.Instance.scenario_manager.getDifficulty()]; i++)
        {
            sca_quantities.Add(48 / NUM_NODES_TO_RESTORE_COLOR[ReferenceAssistor.Instance.scenario_manager.getDifficulty()]);
            sca_molecules.Add(ReferenceAssistor.Instance.scenario_manager.GetComponent<InitBlackAndWhite>().getSpatialCompositionParticleFromRadiation(emitter_radiation_patterns[special_emitter]) + 1); //add 1 to avoid default particle
            sca_colors.Add(intersect_vertical_colors[i]);
        }
        ReferenceAssistor.Instance.module_handlers[0].GetComponent<SpatialCompositionAnalyzer>().setSCAProfile(sca_quantities, sca_molecules, sca_colors, true);

        initializeGateAppearanceRPC(DataConverter.arrayToString(emitter_radiation_patterns), DataConverter.arrayToString(vertical_light_colors), DataConverter.arrayToString(horizontal_light_colors));
    }

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }

    public void shipEnteredBarrier()
    {
        if (stun_sound.isPlaying == false)
        {
            ReferenceAssistor.Instance.spaceship.GetComponent<ShipMovement>().StunShip();
            playStunSoundRPC();
        }
    }

    public void onComputerRegulatorChange()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        updateRadiationVisibilityRPC(ReferenceAssistor.Instance.module_handlers[2].GetComponent<ComputerRegulator>().getProgramActiveState(0, 2));
    }

    public void onEmitterDestroyed(GameObject emitter)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        int emitter_index = radiation_emitters.IndexOf(emitter);
        if (emitter_index != -1)
        {
            emitterDestroyedRPC(emitter_index);
            if (color_restored == false)
            {
                restoreColorRPC();
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void playStunSoundRPC()
    {
        stun_sound.Play();
    }

    [Rpc(SendTo.Everyone)]
    private void initializeGateAppearanceRPC(string radiation_locations, string vert_colors, string horiz_colors)
    {
        //set radiation appearances to designated locations
        emitter_radiation_patterns = DataConverter.stringToArray(radiation_locations);
        for (int i = 0; i < 6; i++)
        {
            radiation_emitters[i].transform.GetChild(0).GetComponent<MeshFilter>().mesh = radiation_options[emitter_radiation_patterns[i]];
        }

        //set vertical light beam colors and materials
        vertical_light_colors = DataConverter.stringToArray(vert_colors);
        for (int i = 0; i < 9; i++)
        {
            vertical_light_beams.transform.GetChild(i).GetComponent<Renderer>().material = light_beam_material_options[vertical_light_colors[i]];
        }

        //set horizontal light beam colors and materials
        horizontal_light_colors = DataConverter.stringToArray(vert_colors);
        for (int i = 0; i < 7; i++)
        {
            horizontal_light_beams.transform.GetChild(i).GetComponent<Renderer>().material = light_beam_material_options[horizontal_light_colors[i]];
        }
    }

    [Rpc(SendTo.Everyone)]
    private void updateRadiationVisibilityRPC(bool visible)
    {
        for (int i = 0; i < 6; i++)
        {
            radiation_emitters[i].transform.GetChild(0).gameObject.SetActive(emitter_active_states[i] == true && visible == true);
        }
    }

    IEnumerator restoreColor()
    {
        float anim_time = COLOR_RESTORATION_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            color_adjustments.saturation.Override(Mathf.Lerp(0.0f, -100.0f, anim_time / COLOR_RESTORATION_TIME));

            yield return null;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void emitterDestroyedRPC(int index)
    {
        if (radiation_emitters[index] != null)
        {
            GameObject.Destroy(radiation_emitters[index]);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void restoreColorRPC()
    {
        color_restored = true;
        StartCoroutine(restoreColor());
    }
}