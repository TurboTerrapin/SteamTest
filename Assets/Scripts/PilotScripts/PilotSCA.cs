/*
    PilotSCA.cs
    - Updates SCA reset bar
    - Updates SCA circular screen
    Contributor(s): Jake Schott
    Last Updated: 9/7/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PilotSCA : NetworkBehaviour, IPowerable
{
    //CLASS CONSTANTS
    private static float RESET_TIMER = 10.0f; //seconds
    private static float PARTICLE_ROTATION_SPEED = 25.0f;
    private static List<int> DEFAULT_MOLECULES = new List<int>() { 0 };
    private static List<int> DEFAULT_MOLECULE_QUANTITIES = new List<int>() { 49 };

    public GameObject reset_bar;
    public GameObject SCA_display;

    private bool is_powered = false;
    private List<int> current_molecules = new List<int>(); //indices of the molecules in the SCA
    private List<int> molecule_quantities = new List<int>(); //corresponds by index to current_molecules

    private Coroutine reset_bar_coroutine = null;

    private void Start()
    {
        resetToDefault();
    }

    //sets SCA to whatever is in DEFAULT_MOLECULES and DEFAULT_MOLECULE_QUANTITIES
    public void resetToDefault()
    {
        current_molecules.Clear();
        molecule_quantities.Clear();

        for (int i = 0; i < DEFAULT_MOLECULES.Count; i++)
        {
            current_molecules.Add(DEFAULT_MOLECULES[i]);
            molecule_quantities.Add(DEFAULT_MOLECULE_QUANTITIES[i]);
        }

        if (reset_bar_coroutine != null)
        {
            StopCoroutine(reset_bar_coroutine);
            reset_bar_coroutine = null;
        }

        if (NetworkManager.Singleton.IsHost && is_powered == true)
        {
            generateNewMolecules();
            transmitNewLoopRPC();
        }
    }

    private void displaySCA(float canvas_rotation, int[] mol_i, int[] mol_l, int[] mol_r)
    {
        //clear existing molecules
        for (int m = SCA_display.transform.GetChild(0).childCount - 1; m >= 0; m--)
        {
            Object.Destroy(SCA_display.transform.GetChild(0).GetChild(m).gameObject);
        }

        //rotate canvas
        SCA_display.transform.GetChild(0).localRotation = Quaternion.Euler(0.0f, 0.0f, canvas_rotation);

        //instantiate new molecules
        for (int m = 0; m < mol_i.Length; m++)
        {
            GameObject molecule = GameObject.Instantiate(SCA_display.transform.GetChild(1).GetChild(mol_i[m]).gameObject, SCA_display.transform.GetChild(0));
            molecule.transform.localPosition = SCA_display.transform.GetChild(2).GetChild(mol_l[m]).localPosition;
            molecule.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, mol_r[m] * 2.0f);
            molecule.SetActive(true);
        }
    }

    private void generateNewMolecules()
    {
        float canvas_rotation = Random.Range(0.0f, 359.9f);

        int number_of_molecules = 0;
        for (int m = 0; m < molecule_quantities.Count; m++)
        {
            number_of_molecules += molecule_quantities[m];
        }

        //ensure there are less molecules than possible locations
        if (number_of_molecules > SCA_display.transform.GetChild(2).childCount)
        {
            Debug.Log("ERROR: Too many molecules in Spatial Composition Analyzer.");
            transmitNewLoopRPC();
            return;
        }

        List<int> possible_locs = new List<int>();
        for (int i = 0; i < SCA_display.transform.GetChild(2).childCount; i++)
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

        //randomize rotation of each molecule
        int[] current_rots = new int[number_of_molecules];
        for (int m = 0; m < number_of_molecules; m++)
        {
            current_rots[m] = Random.Range(0, 180);
        }

        transmitNewMoleculesRPC(canvas_rotation, DataConverter.arrayToString(current_indices), DataConverter.arrayToString(current_locs), DataConverter.arrayToString(current_rots));
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
            for (int m = SCA_display.transform.GetChild(0).childCount - 1; m > 0; m--)
            {
                SCA_display.transform.GetChild(0).GetChild(m).transform.localRotation =
                    Quaternion.Euler(0.0f, 0.0f, SCA_display.transform.GetChild(0).GetChild(m).transform.localRotation.eulerAngles.z + PARTICLE_ROTATION_SPEED * dt);
            }

            fill_time = Mathf.Max(0.0f, fill_time - dt);
            reset_bar.GetComponent<UnityEngine.UI.Image>().fillAmount = 1.0f - (fill_time / (RESET_TIMER));
            yield return null;
        }

        //if host, start the loop again
        if (NetworkManager.Singleton.IsHost)
        {
            generateNewMolecules();
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        SCA_display.SetActive(true);
        reset_bar.SetActive(true);
        if (NetworkManager.Singleton.IsHost == true)
        {
            generateNewMolecules();
            transmitNewLoopRPC();
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        SCA_display.SetActive(false);
        reset_bar.SetActive(false);
        if (reset_bar_coroutine != null)
        {
            StopCoroutine(reset_bar_coroutine);
            reset_bar_coroutine = null;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitNewMoleculesRPC(float canvas_rot, string molecule_ind, string molecule_loc, string molecule_rot)
    {
        int[] molecule_indices = DataConverter.stringToArray(molecule_ind);
        int[] molecule_locations = DataConverter.stringToArray(molecule_loc);
        int[] molecule_rotations = DataConverter.stringToArray(molecule_rot);
        displaySCA(canvas_rot, molecule_indices, molecule_locations, molecule_rotations);
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
