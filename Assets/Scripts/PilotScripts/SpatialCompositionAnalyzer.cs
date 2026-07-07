/*
    SpatialCompositionAnalyzer.cs
    - Updates SCA reset bar
    - Updates SCA circular screen
    Contributor(s): Jake Schott
    Last Updated: 7/7/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpatialCompositionAnalyzer : NetworkBehaviour, IPowerable, IDescribable
{
    //CLASS CONSTANTS
    private static float RESET_TIMER = 10.0f; //seconds
    private static float PARTICLE_ROTATION_SPEED = 25.0f;
    private static List<int> DEFAULT_MOLECULE_QUANTITIES = new List<int>() { 49 };
    private static List<int> DEFAULT_MOLECULES = new List<int>() { 0 };
    private static List<int> DEFAULT_MOLECULE_COLORS = new List<int>() { 0 };

    //list of all ray target names
    private List<string> RAY_TARGETS = new List<string>()
    {
        "SCA",
        "SCA_reset_timer",
        "SCA_alert_indicator"
    };

    //module titles 
    private static string[] INFO_TITLES = new string[]
    {
        "SPATIAL COMPOSITION ANALYZER",
        "SCA RESET TIMER",
        "SCA ALERT INDICATOR"
    };

    //module additional info, or "" if none
    private static string[] INFO_DESCS = new string[]
    {
        "Describes outside spatial molecular composition. Used to identify gases and other anomalies.",
        "",
        "Flashes orange when there is an unusual reading on the spatial composition analyzer."
    };

    public GameObject reset_bar;
    public GameObject notifier;
    public GameObject SCA_display;
    public List<Sprite> particle_options = null;
    public List<Color> color_options = null;
    public AudioClip SCA_notification;

    private bool is_powered = false;
    private bool notification_necessary = false;
    private List<int> current_molecules = new List<int>(); //indices of the molecules in the SCA
    private List<int> molecule_quantities = new List<int>(); //corresponds by index to current_molecules
    private List<int> molecule_colors = new List<int>(); //corresponds by index to current_molecules
    private List<HUDInfo> corresponding_infos = new List<HUDInfo>();
    private Coroutine reset_bar_coroutine = null;

    private void Start()
    {
        resetToDefault();

        for (int i = 0; i < INFO_TITLES.Length; i++)
        {
            corresponding_infos.Add(new HUDInfo(INFO_TITLES[i]));
            if (INFO_DESCS[i].CompareTo("") != 0)
            {
                corresponding_infos[i].setInfo(INFO_DESCS[i]);
            }
        }
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return corresponding_infos[RAY_TARGETS.IndexOf(current_target.name)];
    }

    //only run by host, sets information for new SCA profile
    public void setSCAProfile(List<int> quantities, List<int> textures, List<int> colors, bool immediate_reset)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        current_molecules.Clear();
        molecule_quantities.Clear();
        molecule_colors.Clear();

        for (int i = 0; i < quantities.Count; i++)
        {
            current_molecules.Add(textures[i]);
            molecule_quantities.Add(quantities[i]);
            molecule_colors.Add(colors[i]);
        }

        notification_necessary = true;
        if (immediate_reset == true)
        {
            generateNewMolecules(true);
        }
    }

    //sets SCA to whatever is in DEFAULT_MOLECULES and DEFAULT_MOLECULE_QUANTITIES
    public void resetToDefault()
    {
        current_molecules.Clear();
        molecule_quantities.Clear();
        molecule_colors.Clear();
        notification_necessary = false;

        for (int i = 0; i < DEFAULT_MOLECULES.Count; i++)
        {
            current_molecules.Add(DEFAULT_MOLECULES[i]);
            molecule_colors.Add(DEFAULT_MOLECULE_COLORS[i]);
            molecule_quantities.Add(DEFAULT_MOLECULE_QUANTITIES[i]);
        }

        if (reset_bar_coroutine != null)
        {
            StopCoroutine(reset_bar_coroutine);
            reset_bar_coroutine = null;
        }

        if (NetworkManager.Singleton.IsHost == true && is_powered == true)
        {
            generateNewMolecules(false);
            transmitNewLoopRPC();
        }
    }

    private void displaySCA(float renderer_rotation, int[] mol_i, int[] mol_c, int[] mol_l, int[] mol_r)
    {
        //play notification if necessary
        if (notification_necessary == true)
        {
            ReferenceAssistor.Instance.audio_manager.AddNotification(0, SCA_notification);
            notification_necessary = false;
        }

        //hide existing molecules
        foreach (Transform molecule in SCA_display.transform.GetChild(0))
        {
            molecule.gameObject.SetActive(false);
        }

        //rotate renderer
        SCA_display.transform.GetChild(0).localRotation = Quaternion.Euler(0.0f, 0.0f, renderer_rotation);

        //instantiate new molecules
        for (int m = 0; m < mol_i.Length; m++)
        {
            GameObject molecule = SCA_display.transform.GetChild(0).GetChild(mol_l[m]).gameObject;
            molecule.GetComponent<SpriteRenderer>().sprite = particle_options[mol_i[m]];
            molecule.GetComponent<SpriteRenderer>().color = color_options[mol_c[m]];
            molecule.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, mol_r[m] * 2.0f);
            molecule.SetActive(true);
        }
    }

    private void generateNewMolecules(bool notify)
    {
        float renderer_rotation = Random.Range(0.0f, 359.9f);

        int number_of_molecules = 0;
        for (int m = 0; m < molecule_quantities.Count; m++)
        {
            number_of_molecules += molecule_quantities[m];
        }

        //ensure there are less molecules than possible locations
        if (number_of_molecules > SCA_display.transform.GetChild(0).childCount)
        {
            Debug.Log("ERROR: Too many molecules in Spatial Composition Analyzer.");
            transmitNewLoopRPC();
            return;
        }

        List<int> possible_locs = new List<int>();
        for (int i = 0; i < SCA_display.transform.GetChild(0).childCount; i++)
        {
            possible_locs.Add(i);
        }

        //randomize locations
        int[] current_locs = new int[number_of_molecules];
        for (int m = 0; m < number_of_molecules; m++)
        {
            int designated_loc = Random.Range(0, possible_locs.Count);
            current_locs[m] = possible_locs[designated_loc];
            possible_locs.RemoveAt(designated_loc);
        }

        //set which molecules are which
        int[] current_indices = new int[number_of_molecules];
        int index = 0;
        for (int i = 0; i < current_molecules.Count; i++)
        {
            for (int m = 0; m < molecule_quantities[i]; m++)
            {
                current_indices[index] = current_molecules[i];
                index++;
            }
        }

        //set colors of each molecule
        int[] current_colors = new int[number_of_molecules];
        index = 0;
        for (int i = 0; i < current_molecules.Count; i++)
        {
            for (int m = 0; m < molecule_quantities[i]; m++)
            {
                current_colors[index] = molecule_colors[i];
                index++;
            }
        }

        //randomize rotation of each molecule
        int[] current_rots = new int[number_of_molecules];
        for (int m = 0; m < number_of_molecules; m++)
        {
            current_rots[m] = Random.Range(0, 180);
        }

        transmitNewMoleculesRPC(renderer_rotation, DataConverter.arrayToString(current_indices), DataConverter.arrayToString(current_colors), DataConverter.arrayToString(current_locs), DataConverter.arrayToString(current_rots), notify);
        transmitNewLoopRPC();
    }

    IEnumerator resetBarUpdater()
    {
        //reset bars
        reset_bar.GetComponent<UnityEngine.UI.Image>().fillAmount = 0.0f;

        //fill the bar, rotate existing molecules
        float fill_time = RESET_TIMER;
        while (fill_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            //rotate existing particles
            float rotate_factor = PARTICLE_ROTATION_SPEED * dt;
            foreach (Transform m in SCA_display.transform.GetChild(0))
            {
                m.transform.Rotate(0.0f, 0.0f, rotate_factor);
            }

            fill_time = Mathf.Max(0.0f, fill_time - dt);
            reset_bar.GetComponent<UnityEngine.UI.Image>().fillAmount = 1.0f - (fill_time / (RESET_TIMER));
            yield return null;
        }

        //if host, start the loop again
        if (NetworkManager.Singleton.IsHost == true)
        {
            generateNewMolecules(notification_necessary);
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        SCA_display.SetActive(true);
        reset_bar.SetActive(true);
        notifier.SetActive(true);
        if (NetworkManager.Singleton.IsHost == true)
        {
            generateNewMolecules(notification_necessary);
            transmitNewLoopRPC();
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        SCA_display.SetActive(false);
        reset_bar.SetActive(false);
        notifier.SetActive(false);
        if (reset_bar_coroutine != null)
        {
            StopCoroutine(reset_bar_coroutine);
            reset_bar_coroutine = null;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitNewMoleculesRPC(float canvas_rot, string molecule_ind, string molecule_col, string molecule_loc, string molecule_rot, bool notify)
    {
        int[] indices = DataConverter.stringToArray(molecule_ind);
        int[] colors = DataConverter.stringToArray(molecule_col);
        int[] locations = DataConverter.stringToArray(molecule_loc);
        int[] rotations = DataConverter.stringToArray(molecule_rot);

        notification_necessary = notify;
        displaySCA(canvas_rot, indices, colors, locations, rotations);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitNewLoopRPC()
    {
        if (reset_bar_coroutine != null)
        {
            StopCoroutine(reset_bar_coroutine);
        }
        reset_bar_coroutine = StartCoroutine(resetBarUpdater());
    }
}