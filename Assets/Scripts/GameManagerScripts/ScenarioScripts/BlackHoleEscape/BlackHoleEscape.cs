/*
    BlackHole.cs
    Contributor(s): Henryk Musial
    Last Updated: 4/1/2026
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BlackHoleEscape : NetworkBehaviour, IScenario
{
    //CLASS CONSTANTS
    //private static int SEEKER_MINE_QUANTITY = 20;
    private static string DEATH_MESSAGE = "You died buddy get better at this game";


    public void initiateScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
            return;


        Transform world_root = GameObject.FindGameObjectWithTag("WorldRoot").transform;

    }

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }
}