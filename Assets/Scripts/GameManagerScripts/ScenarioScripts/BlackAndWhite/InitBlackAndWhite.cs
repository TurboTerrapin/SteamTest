/*
    InitBlackAndWhite.cs
    - Used for initializing permanent information on BlackAndWhite (SCA to emitter options)
    Contributor(s): Jake Schott
    Last Updated: 8/20/2026
*/

using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

public class InitBlackAndWhite : NetworkBehaviour, IScenarioInitialization
{
    public static int SCENARIO_DATABASE_INDEX = 2;

    public List<GameObject> sca_button_options = null;
    public List<Texture> radiation_pattern_textures = null;

    private List<int> corresponding_radiation_options = new List<int>() { -1, -1, -1, -1, -1, -1 };

    public void initializeDatabaseInformation()
    {
        List<int> remaining_options = new List<int>() { 0, 1, 2, 3, 4, 5 };
        for (int i = 0; i < 6; i++)
        {
            int current_option = remaining_options[Random.Range(0, remaining_options.Count)];
            remaining_options.Remove(current_option);
            corresponding_radiation_options[i] = current_option;
        }

        transmitParticlesAndRadiationLinkageRPC(DataConverter.listToString(corresponding_radiation_options));
    }

    public int getSpatialCompositionParticleFromRadiation(int radiation_index)
    {
        return corresponding_radiation_options.IndexOf(radiation_index);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitParticlesAndRadiationLinkageRPC(string particle_radiation_matches)
    {
        int[] temp_matches = DataConverter.stringToArray(particle_radiation_matches);
        for (int i = 0; i < 6; i++)
        {
            corresponding_radiation_options[i] = temp_matches[i];
            sca_button_options[i].GetComponentAtIndex<ManualTextureLinker>(5).setTexture(radiation_pattern_textures[corresponding_radiation_options[i]]);
        }
    }
}