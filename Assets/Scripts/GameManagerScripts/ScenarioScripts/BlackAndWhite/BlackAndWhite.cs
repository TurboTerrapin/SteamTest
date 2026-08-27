/*
    BlackAndWhite.cs
    - Handles all the functions pertaining to the black-and-white scenario (the wall one)
    Contributor(s): Jake Schott
    Last Updated: 8/23/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class BlackAndWhite : NetworkBehaviour, IScenario, IComputerRegulatorSusceptible, IUniversalCommunicable
{
    //CLASS CONSTANTS
    private static string DEATH_MESSAGE = "Stolen ship SCC-3002 was found destroyed near an anomalous barrier. Crew was unable to disable the wall without sustaining critical damage. No survivors were found.";
    private static Color[] LIGHT_BEAM_COLORS_OPTIONS = new Color[9] { ReferenceAssistor.COLOR_OPTIONS[0], ReferenceAssistor.COLOR_OPTIONS[1], ReferenceAssistor.COLOR_OPTIONS[2], ReferenceAssistor.COLOR_OPTIONS[3], Color.white, Color.red, Color.yellow, Color.blue, new Color(1.0f, 0.0f, 0.5f) };
    private static float COLOR_RESTORATION_TIME = 5.0f;
    private static float SHIELD_OSCILLATION_REFRESH_SPEED = 1.0f;
    private static int CENTER_INDEX = 8;
    private static float CENTER_SPEED = 55.0f;
    private static List<float> RING_SPEEDS = new List<float>() { 10.0f, 15.0f, 10.0f, 15.0f };
    private static int[] NUM_NODES_TO_RESTORE_COLOR = new int[] { 1, 2, 3, 4 }; //corresponds to easy, medium, hard, expert difficulties

    public Material emitter_shield_material;
    public List<Material> light_beam_material_options = null;  
    public List<Mesh> radiation_options = null;
    public GameObject token;
    public List<GameObject> radiation_emitters = null;
    public List<GameObject> emitter_shield_generators = null;
    public List<GameObject> emitter_shields = null;
    public GameObject vertical_light_beams;
    public GameObject horizontal_light_beams;
    public GameObject extended_light_beams;
    public GameObject control_nodes;
    public BWWall ship_barrier;
    public GameObject wall_extensions;

    private bool color_restored = false;
    private bool barrier_disabled = false;
    private string token_serial_number = "";
    private bool[] enabled_emitter_shields = new bool[2] { true, true };
    private int special_emitter = -1; //the emitter that must be destroyed to bring color back
    private int[] emitter_radiation_patterns = new int[6] { -1, -1, -1, -1, -1, -1 }; //corresponds to A, B, C, D, E, and F mesh options
    private bool[] emitter_active_states = new bool[6] { true, true, true, true, true, true };
    private List<int> intersect_vertical_colors = new List<int>();
    private int intersect_horizontal_color;
    private List<int> intersect_node_indexes = new List<int>();
    private int[] vertical_light_colors = new int[9] { -1, -1,-1, -1, -1, -1, -1, -1, -1 };
    private int[] horizontal_light_colors = new int[7] { -1, -1, -1, -1, -1, -1, -1 };
    private UnityEngine.Rendering.Universal.ColorAdjustments color_adjustments;

    private void Start()
    {
        //prepare emitter shield material
        emitter_shield_material = new Material(emitter_shield_material);

        //oscillate shield brightness
        emitter_shields[0].GetComponent<MeshRenderer>().material = emitter_shield_material;
        emitter_shields[1].GetComponent<MeshRenderer>().material = emitter_shield_material;
        StartCoroutine(shieldBrightnessOscillator());
    }

    IEnumerator shieldBrightnessOscillator()
    {
        float elapsed_time = 0.0f;
        Color darker_blue = new Color(0.0f, 0.1f, 0.15f);
        Color lighter_blue = new Color(0.0f, 0.22f, 0.3f);
        while (enabled_emitter_shields[0] == true || enabled_emitter_shields[1] == true)
        {
            elapsed_time += Time.deltaTime * SHIELD_OSCILLATION_REFRESH_SPEED;
            float oscillation_progress = Mathf.Lerp(0.2f, 1.0f, Mathf.PingPong(elapsed_time, 1.0f));
            emitter_shield_material.SetColor("_EmissionColor", Color.Lerp(darker_blue, lighter_blue, oscillation_progress));

            yield return null;
        }
    }

    private void OnDisable()
    {
        Destroy(emitter_shield_material);
    }

    //only run by host
    public void prepScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        //set token serial number and position
        token.transform.localPosition = new Vector3(Random.Range(-250.0f, 250.0f), Random.Range(-50.0f, 50.0f), Random.Range(-250.0f, 250.0f) + token.transform.localPosition.z);
        token.transform.localRotation = Quaternion.Euler(Random.Range(0.0f, 180.0f), Random.Range(0.0f, 180.0f), Random.Range(0.0f, 180.0f));

        //prevent collectible items from spawning into token or barrier
        List<OffLimitsSpawnLocation> off_limits_locations = new List<OffLimitsSpawnLocation>();
        off_limits_locations.Add(new OffLimitsSpawnLocation(token.transform.localPosition, 200.0f)); //add token
        for (int i = 0; i < 33; i++)
        {
            off_limits_locations.Add(new OffLimitsSpawnLocation(new Vector3((i * 150.0f) + -2400.0f, 0.0f, 2500.0f), 500.0f)); //add barrier point
        }
        List<Vector3> spawn_locations = ReferenceAssistor.Instance.scenario_manager.generateSpawnLocations(50.0f, 0, off_limits_locations);
    }

    public void initiateScenario()
    {
        //set screen to black-and-white
        ReferenceAssistor.Instance.camera_settings.GetComponent<Volume>().profile.TryGet(out color_adjustments);
        color_adjustments.active = true;
        color_adjustments.saturation.Override(-100.0f);

        //enable light layer two
        ReferenceAssistor.Instance.light_layer_two.gameObject.SetActive(true);
        foreach (Transform t in ReferenceAssistor.Instance.light_layer_two.transform)
        {
            Light l = t.GetComponent<Light>();
            l.intensity = 0.25f;
            l.color = Color.white;
        }

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

        //randomize extended light beams
        List<int> extended_light_colors = new List<int>();
        for (int i = 0; i < extended_light_beams.transform.childCount; i++)
        {
            extended_light_colors.Add(Random.Range(0, 9));
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

        //determine index of horizontal light color selected for intersection
        int horizontal_index = -1;
        for (int i = 0; i < horizontal_light_colors.Length; i++)
        {
            if (horizontal_light_colors[i] == intersect_horizontal_color)
            {
                horizontal_index = i;
                break;
            }
        }

        //determine indexes of vertical light colors selected for intersection and add to intersect node list
        for (int i = 0; i < intersect_vertical_colors.Count; i++)
        {
            int vertical_index = -1;
            for (int c = 0; c < vertical_light_colors.Length; c++)
            {
                if (vertical_light_colors[c] == intersect_vertical_colors[i])
                {
                    vertical_index = c;
                    break;
                }
            }

            intersect_node_indexes.Add((horizontal_index * 9) + vertical_index);
        }

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

        //finalize information and send it out to everyone
        initializeBlackAndWhiteRPC(DataConverter.arrayToString(emitter_radiation_patterns), DataConverter.arrayToString(vertical_light_colors), DataConverter.arrayToString(horizontal_light_colors), DataConverter.listToString(extended_light_colors), intersect_horizontal_color, ReferenceAssistor.Instance.spaceship.GetComponent<ShipInventory>().generateSerialNumber());
    }

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }

    public string getTokenSerialNumber()
    {
        return token_serial_number;
    }

    private bool isTokenSerialNumberMessage(List<int> code_indexes, List<int> code_is_numeric)
    {
        string message_to_check = "";
        for (int i = 0; i < code_indexes.Count; i++)
        {
            if (code_is_numeric[i] == 1)
            {
                message_to_check += code_indexes[i];
            }
            else
            {
                message_to_check += "X";
            }
        }

        string correct_message = token_serial_number.Replace(" ", "");
        return message_to_check.Contains(correct_message);
    }

    public bool checkTransmission(float frequency, List<int> code_indexes, List<int> code_is_numeric, int code_color)
    {
        return isTokenSerialNumberMessage(code_indexes, code_is_numeric);
    }

    public void handleTransmission(float frequency, List<int> code_indexes, List<int> code_is_numeric, int code_color)
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (isTokenSerialNumberMessage(code_indexes, code_is_numeric) == true)
            {
                emitterFlashRPC(special_emitter);
            }
        }
    }

    public void shipEnteredBarrier()
    {
        if (GetComponent<AudioSource>().isPlaying == false)
        {
            ReferenceAssistor.Instance.spaceship.GetComponent<ShipMovement>().StunShip();
            playElectricStunSoundRPC();
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

    public void onShieldGeneratorDisabled(GameObject generator)
    {
        if (emitter_shield_generators.IndexOf(generator) != -1)
        {
            emitter_shield_generators[emitter_shield_generators.IndexOf(generator)] = null;
            for (int i = 0; i < 2; i++)
            {
                if (enabled_emitter_shields[i] == true)
                {
                    if (emitter_shield_generators[i * 2] == null && emitter_shield_generators[(i* 2) + 1] == null)
                    {
                        shieldDisabledRPC(i);
                    }
                }
            }
        }
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
            if (color_restored == false && special_emitter == emitter_index)
            {
                restoreColorRPC();
            }
        }
    }

    public void onNodeDestroyed(int node_index)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        //check if node is an intersection node
        if (node_index != -1)
        {
            if (intersect_node_indexes.Contains(node_index) == true)
            {
                intersect_node_indexes.Remove(node_index);
                if (intersect_node_indexes.Count == 0 && barrier_disabled == false)
                {
                    barrierDisabledRPC();
                }
            }
        }
    }

    IEnumerator barrierDisableFlicker()
    {
        for (int i = 0; i < 3; i++)
        {
            extended_light_beams.gameObject.SetActive(true);
            vertical_light_beams.gameObject.SetActive(true);
            horizontal_light_beams.gameObject.SetActive(true);
            foreach (Transform n in control_nodes.transform)
            {
                n.GetComponent<BWControlNode>().activate();
            }

            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));

            extended_light_beams.gameObject.SetActive(false);
            vertical_light_beams.gameObject.SetActive(false);
            horizontal_light_beams.gameObject.SetActive(false);
            foreach (Transform n in control_nodes.transform)
            {
                n.GetComponent<BWControlNode>().deactivate();
            }

            yield return new WaitForSeconds(Random.Range(0.05f, 0.35f));
        }
    }

    IEnumerator shieldDisableFlash(int index)
    {
        for (int i = 0; i < 3; i++)
        {
            emitter_shields[index].SetActive(true);

            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));

            emitter_shields[index].SetActive(false);

            yield return new WaitForSeconds(Random.Range(0.05f, 0.35f));
        }
    }

    [Rpc(SendTo.Everyone)]
    private void playElectricStunSoundRPC()
    {
        GetComponent<AudioSource>().Play();
    }

    [Rpc(SendTo.Everyone)]
    private void initializeBlackAndWhiteRPC(string radiation_locations, string vert_colors, string horiz_colors, string ext_colors, int energy_pattern_color, string t_serial_number)
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
        horizontal_light_colors = DataConverter.stringToArray(horiz_colors);
        for (int i = 0; i < 7; i++)
        {
            horizontal_light_beams.transform.GetChild(i).GetComponent<Renderer>().material = light_beam_material_options[horizontal_light_colors[i]];
        }

        //set extended light beam materials
        int[] extended_light_colors = DataConverter.stringToArray(ext_colors);
        for (int i = 0; i < extended_light_beams.transform.childCount; i++)
        {
            extended_light_beams.transform.GetChild(i).GetComponent<MeshRenderer>().material = light_beam_material_options[extended_light_colors[i]];
        }

        //initialize energy pattern
        PatternData RLGLpattern = new PatternData();
        RLGLpattern.setCenter(CENTER_INDEX, energy_pattern_color, CENTER_SPEED);
        RLGLpattern.setRings(4, new List<int>() { energy_pattern_color, energy_pattern_color, energy_pattern_color, energy_pattern_color}, new List<int>() { 1, 2, 1, 2}, new List<bool>(){ false, false, false, false}, RING_SPEEDS);

        ReferenceAssistor.Instance.module_handlers[2].GetComponent<EnergyPattern>().setPattern(RLGLpattern);

        //set token serial number
        token_serial_number = t_serial_number;
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
    private void emitterFlashRPC(int index)
    {
        if (radiation_emitters[index] != null)
        {
            radiation_emitters[index].GetComponent<BWEmitter>().enableFlash();
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
    private void shieldDisabledRPC(int index)
    {
        enabled_emitter_shields[index] = false;
        emitter_shields[index].SetActive(false);
        for (int i = 0; i < 3; i++)
        {
            if (radiation_emitters[i + (index * 3)] != null)
            {
                radiation_emitters[i + (index * 3)].GetComponent<BWEmitter>().onProtectiveShieldsDiabled();
            }
        }
        StartCoroutine(shieldDisableFlash(index));
    }

    [Rpc(SendTo.Everyone)]
    private void restoreColorRPC()
    {
        color_restored = true;
        StartCoroutine(restoreColor());
    }

    [Rpc(SendTo.Everyone)]
    private void barrierDisabledRPC()
    {
        barrier_disabled = true;

        //play sound
        wall_extensions.GetComponent<AudioSource>().Play();

        //disable main barrier but enable secondary barriers
        if (NetworkManager.Singleton.IsHost == true)
        {
            ship_barrier.gameObject.SetActive(false);
            foreach (Transform c in wall_extensions.transform)
            {
                c.GetComponent<BWWall>().activate();
            }
        }

        //hide red map icons
        foreach (Transform t in ReferenceAssistor.Instance.world_root.transform)
        {
            if (t.gameObject.name.CompareTo("MapIndicator") == 0)
            {
                GameObject.Destroy(t.gameObject);
            }
        }

        StartCoroutine(barrierDisableFlicker());
    }
}