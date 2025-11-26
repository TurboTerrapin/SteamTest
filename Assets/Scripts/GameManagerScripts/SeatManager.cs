/*
    SeatManager.cs
    - Used to ensure two players are not sitting in the same seat at the same time
    - Checks if a player is close enough to sit down
    - Handles RPC which positions the seats
    - Handles giving sit down/get up directions for physical seats
    - Handles storing/giving seat indexes (where they are shifted)
    Contributor(s): Jake Schott
    Last Updated: 11/26/2025
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SeatManager : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float SIT_RANGE = 0.5f;
    public static Vector2[][] SEAT_COORDINATES = new Vector2[4][]{
        new Vector2[]{ new Vector2(-0.92f, 0.0f), new Vector2(0.0f, 0.0f) }, //pilot seat positions
        new Vector2[]{ new Vector2(0.0f, 0.0f), new Vector2(0.92f, 0.0f) }, //tactician seat positions
        new Vector2[]{ new Vector2(0.0f, 0.0f), new Vector2(-0.6f, -0.6f), new Vector2(-1.1f, -1.1f), new Vector2(-1.7f, -1.7f), new Vector2(-2.05f, -2.05f)}, //engineer seat positions
        new Vector2[]{} //captain seat positions
    };

    //GAME OBJECTS
    public List<GameObject> physical_seats = null;
    private PlayerManager player_manager;

    private int[] occupied_seats = new int[4] { -1, -1, -1, -1 }; //corresponds to player index (ex. if occupied_seats[0] is 1, that means player #2 is in the pilot seat)
    private int[] seat_indexes = new int[4] { 1, 0, 0, -1 }; //goes left-to-right from 0 to # of possible seat positions (minus one), -1 for captain because no shifting

    private void Start()
    {
        player_manager = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
    }

    //returns -1 if no unoccupied seats within SIT_RANGE, otherwise returns index (0-3) of position available
    public int checkSeats(Vector3 player_pos)
    {
        int closest_pos = -1;
        for (int i = 0; i < 4; i++)
        {
            if (occupied_seats[i] == -1)
            {
                if (i == 3)
                {
                    if (Vector3.Distance(player_pos, physical_seats[3].transform.GetChild(0).position) < SIT_RANGE)
                    {
                        closest_pos = 3;
                    }
                }
                else
                {
                    bool left_check = Vector3.Distance(player_pos, physical_seats[i].transform.GetChild(0).position) < SIT_RANGE;
                    bool right_check = Vector3.Distance(player_pos, physical_seats[i].transform.GetChild(1).position) < SIT_RANGE;
                    if (seat_indexes[i] == 0) //seat is shifted all the way to the left
                    {
                        if (right_check == true)
                        {
                            closest_pos = i;
                        }
                    }
                    else if (seat_indexes[i] == SEAT_COORDINATES[i].Length - 1) //seat is shifted all the way to the right
                    {
                        if (left_check == true)
                        {
                            closest_pos = i;
                        }
                    }
                    else //seat is somewhere between left and right
                    {
                        if (left_check == true || right_check == true)
                        {
                            closest_pos = i;
                        }
                    }
                }
            }
        }

        return closest_pos;
    }

    public GameObject getSitDownPosition(int pos, Vector3 player_pos)
    {
        bool direction = getSitDownDirection(pos, player_pos);
        if (direction == true) //needs to sit left, send right
        {
            return physical_seats[pos].transform.GetChild(1).gameObject;
        }
        return physical_seats[pos].transform.GetChild(0).gameObject; //needs to sit right, send left
    }

    //true is left, false is right
    public bool getSitDownDirection(int pos, Vector3 player_pos)
    {
        if (seat_indexes[pos] == 0) //seat to the left
        {
            return true;
        }
        else if (seat_indexes[pos] == SEAT_COORDINATES.Length - 1) //seat to the right, send left
        {
            return false;
        }

        //else, pick whichever is closest to player (could be left or right)
        if (Vector3.Distance(player_pos, physical_seats[pos].transform.GetChild(0).position) < Vector3.Distance(player_pos, physical_seats[pos].transform.GetChild(1).position))
        {
            return false;
        }
        return true;
    }

    //true is left, false is right
    public bool getGetUpDirection(int pos)
    {
        if (seat_indexes[pos] == 0)
        {
            return true;
        }
        return false;
    }

    //called to trigger an RPC to occupy a seat
    public bool sitDown(int seat)
    {
        if (occupied_seats[seat] != -1)
        {
            return false;
        }
        transmitSeatOccupantChangeRPC(seat, player_manager.getPlayerIndex(), true);
        return true;
    }

    //returns the SEAT_COORDINATES index based on whether the seat is farthest left, farthest right, or if in the middle, look direction (left = false)
    public int getShiftLocation(int pos, bool look_direction)
    {
        if (pos == 0 || pos == 1)
        {
            int new_seat_index = 0;
            if (seat_indexes[pos] == 0) //if left, then right
            {
                new_seat_index = 1;
            }
            return new_seat_index; //left
        }
        if (seat_indexes[pos] == 0) //if furthest left, one to the right
        {
            return 1;
        }
        if (seat_indexes[pos] == SEAT_COORDINATES[pos].Length - 1) //if furthest right, one to the left
        {
            return SEAT_COORDINATES[pos].Length - 2;
        }
        if (look_direction == true) //if looking right
        {
            return seat_indexes[pos] + 1; //right
        }
        return seat_indexes[pos] - 1; //left
    }

    //called by shifting player after shift to seat transform
    public void updateSeatLocation(int seat, Vector3 new_seat_loc)
    {
        transmitSeatLocationChangeRPC(seat, new_seat_loc);
    }

    //called by shifting player after shift to a new SEAT_LOCATION
    public void updateSeatIndex(int seat, int new_seat_index)
    {
        transmitSeatIndexChangeRPC(seat, new_seat_index);
    }

    //called to trigger an RPC to relinquish a seat
    public bool getUp(int seat)
    {
        if (occupied_seats[seat] == player_manager.getPlayerIndex())
        {
            transmitSeatOccupantChangeRPC(seat, player_manager.getPlayerIndex(), false);
            return true;
        }
        return false;
    }

   
    [Rpc(SendTo.Everyone)]
    private void transmitSeatOccupantChangeRPC(int seat, int occupant, bool occupied)
    {
        if (occupied == true)
        {
            occupied_seats[seat] = occupant;
            player_manager.freezePlayer(occupant);
        }
        else
        {
            occupied_seats[seat] = -1;
            player_manager.unfreezePlayer(occupant);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSeatIndexChangeRPC(int seat, int new_seat_index)
    {
        seat_indexes[seat] = new_seat_index;
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSeatLocationChangeRPC(int seat, Vector3 new_seat_loc)
    {
        physical_seats[seat].transform.localPosition = new_seat_loc;
    }
}