/*
    SeatManager.cs
    - Used to ensure two players are not sitting in the same seat at the same time
    - Checks if a player is close enough to sit down
    Contributor(s): Jake Schott
    Last Updated: 8/3/2025
*/

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SeatManager : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float[] SIT_RANGES = new float[4] { 0.5f, 0.5f, 1.5f, 0.5f }; //pilot, tactician, engineer, captain

    //GAME OBJECTS
    public List<GameObject> position_point_holders = null;

    private bool[] occupied_seats = new bool[4] { false, false, false, false };

    public int checkSeats(Vector3 player_pos)
    {
        float closest_dist = 9999.9f;
        int closest_pos = -1;
        for (int i = 0; i < position_point_holders.Count; i++)
        {
            //check the 0 child on the holder for the proximity
            float test_dist = Vector3.Distance(player_pos, position_point_holders[i].transform.GetChild(0).position);
            if (test_dist < closest_dist)
            {
                if (test_dist < SIT_RANGES[i] && occupied_seats[i] == false)
                {
                    closest_dist = test_dist;
                    closest_pos = i;
                }
            }
        }
        return closest_pos;
    }

    public bool sitDown(Vector3 player_pos)
    {
        int seat = checkSeats(player_pos);
        if (seat >= 0)
        {
            transmitSeatChangeRPC(seat, true);
            return true;
        }
        return false;
    }

    public bool getUp(int seat)
    {
        if (occupied_seats[seat] == true)
        {
            transmitSeatChangeRPC(seat, false);
            return true;
        }
        return false;
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSeatChangeRPC(int seat, bool occupied)
    {
        occupied_seats[seat] = occupied;
    }
}
