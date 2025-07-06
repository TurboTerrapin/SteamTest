/*
    PilotSCA.cs
    - Updates SCA reset bar
    - Updates SCA circular screen
    Contributor(s): Jake Schott
    Last Updated: 7/5/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PilotSCA : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float RESET_TIMER = 10.0f; //seconds
    private static float PARTICLE_ROTATION_SPEED = 25.0f;

    public GameObject reset_bar;
    public GameObject SCA_canvas;

    private List<int> current_molecules = new List<int>(); //indices of the molecules in the SCA
    private List<int> molecule_quantities = new List<int>(); //corresponds by index to current_molecules

    private Coroutine reset_bar_coroutine = null;

    private void displaySCA(float canvas_rotation, int[] mol_i, int[] mol_l, int[] mol_r)
    {
        //clear existing molecules
        for (int m = SCA_canvas.transform.GetChild(1).childCount - 1; m >= 0; m--)
        {
            Object.Destroy(SCA_canvas.transform.GetChild(1).GetChild(m).gameObject);
        }

        //rotate canvas
        SCA_canvas.transform.GetChild(1).localRotation = Quaternion.Euler(0.0f, 0.0f, canvas_rotation);

        //instantiate new molecules
        for (int m = 0; m < mol_i.Length; m++)
        {
            GameObject molecule = GameObject.Instantiate(SCA_canvas.transform.GetChild(2).GetChild(mol_i[m]).gameObject, SCA_canvas.transform.GetChild(1));
            molecule.transform.localPosition = SCA_canvas.transform.GetChild(3).GetChild(mol_l[m]).localPosition;
            molecule.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, mol_r[m] * 2.0f);
            molecule.SetActive(true);
        }
    }

    private string arrayToString(int[] to_convert)
    {
        string to_return = "";
        for (int i = 0; i < to_convert.Length; i++)
        {
            to_return += (char)to_convert[i];
        }
        return to_return;
    }

    private int[] stringToArray(string to_convert)
    {
        int[] return_array = new int[to_convert.Length];
        for (int i = 0; i < to_convert.Length; i++)
        {
            return_array[i] = (int)to_convert[i];
        }
        return return_array;
    }

    private void generateNewMolecules()
    {
        float canvas_rotation = Random.Range(0.0f, 359.9f);

        int number_of_molecules = 0;
        for (int m = 0; m < molecule_quantities.Count; m++)
        {
            number_of_molecules += molecule_quantities[m];
            Debug.Log(number_of_molecules);
        }

        //ensure there are less molecules than possible locations
        if (number_of_molecules > SCA_canvas.transform.GetChild(3).childCount)
        {
            Debug.Log("ERROR: Too many molecules in Spatial Composition Analyzer.");
            transmitNewLoopRPC();
            return;
        }

        List<int> possible_locs = new List<int>();
        for (int i = 0; i < SCA_canvas.transform.GetChild(3).childCount; i++)
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

        transmitNewMoleculesRPC(canvas_rotation, arrayToString(current_indices), arrayToString(current_locs), arrayToString(current_rots));
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
            for (int m = SCA_canvas.transform.GetChild(1).childCount - 1; m > 0; m--)
            {
                SCA_canvas.transform.GetChild(1).GetChild(m).transform.localRotation =
                    Quaternion.Euler(0.0f, 0.0f, SCA_canvas.transform.GetChild(1).GetChild(m).transform.localRotation.eulerAngles.z + PARTICLE_ROTATION_SPEED * dt);
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

    private void Start()
    {
        current_molecules.Add(0);
        molecule_quantities.Add(49);
        if (NetworkManager.Singleton.IsHost)
        {
            generateNewMolecules();
            transmitNewLoopRPC();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitNewMoleculesRPC(float canvas_rot, string molecule_ind, string molecule_loc, string molecule_rot)
    {
        int[] molecule_indices = stringToArray(molecule_ind);
        int[] molecule_locations = stringToArray(molecule_loc);
        int[] molecule_rotations = stringToArray(molecule_rot);
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
