/*
    BlackAndWhite.cs
    - Handles all the functions pertaining to the black-and-white scenario (the wall one)
    Contributor(s): Jake Schott
    Last Updated: 7/3/2026
*/

using Unity.Netcode;
using UnityEngine;

public class BlackAndWhite : NetworkBehaviour, IScenario
{
    //CLASS CONSTANTS
    private static string DEATH_MESSAGE = "Stolen ship SEACC-3002 was found destroyed near an anomalous barrier. Crew was unable to disable the wall without sustaining critical damage. No survivors were found.";

    public AudioSource stun_sound;
    public BlackAndWhiteWall ship_barrier;

    public void initiateScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        ship_barrier.activate();
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

    [Rpc(SendTo.Everyone)]
    private void playStunSoundRPC()
    {
        stun_sound.Play();
    }
}