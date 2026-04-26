/*
    SeatManager.cs
    - Used to ensure two players are not sitting in the same seat at the same time
    - Checks if a player is close enough to sit down
    - Handles RPCs which position the seats
    - Handles giving sit down/get up directions for physical seats
    - Handles storing/giving seat indexes (where they are shifted)
    - Handles weird captain chair mechanics (the moving parts)
    Contributor(s): Jake Schott
    Last Updated: 4/25/2026
*/

using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
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
    public List<GameObject> seat_prefabs = null;
    public List<GameObject> captain_flipouts = null; //0 is port, 1 is starboard
    public GameObject captain_retractables;
    private PlayerManager player_manager;
    private PowerControl power_control;

    private ulong[] occupied_seats = new ulong[4] { 0, 0, 0, 0 }; //corresponds to player's steam ID (will be 0 if unoccupied)
    private int[] seat_indexes = new int[4] { 1, 0, 0, -1 }; //goes left-to-right from 0 to # of possible seat positions (minus one), -1 for captain because no shifting
    private ulong[] seat_ids = new ulong[4] { 0, 0, 0, 0 }; //keep track of seats' network object IDs
    private Coroutine captain_seat_transformation_coroutine = null;

    private void Start()
    {
        player_manager = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
        power_control = ReferenceAssistor.Instance.module_handlers[4].GetComponent<PowerControl>();
    }

    //destroy seats
    public void destroySeats()
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            foreach (GameObject seat in physical_seats)
            {
                if (seat.GetComponent<NetworkObject>() != null)
                {
                    seat.GetComponent<NetworkObject>().Despawn(true);
                }
            }
        }
    }

    //returns -1 if no unoccupied seats within SIT_RANGE, otherwise returns index (0-3) of position available
    public int checkSeats(Vector3 player_pos)
    {
        int closest_pos = -1;
        for (int i = 0; i < 4; i++)
        {
            if (occupied_seats[i] == 0)
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
        else if (seat_indexes[pos] == SEAT_COORDINATES.Length) //seat to the right, send left
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
        if (occupied_seats[seat] != 0)
        {
            return false;
        }
        transmitSeatOccupantChangeRPC(seat, NetworkManager.Singleton.LocalClientId, SteamClient.SteamId, true);
        return true;
    }

    //returns true if able to shift left
    public bool canShiftLeft(int pos)
    {
        return (seat_indexes[pos] > 0);
    }

    //returns true if able to shift right
    public bool canShiftRight(int pos)
    {
        return (seat_indexes[pos] < (SEAT_COORDINATES[pos].Length - 1));
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

    //called by shifting player after shift to a new SEAT_LOCATION
    public void updateSeatIndex(int seat, int new_seat_index)
    {
        transmitSeatIndexChangeRPC(seat, new_seat_index);
    }

    //called to trigger an RPC to relinquish a seat
    public bool getUp(int seat)
    {
        if (occupied_seats[seat] == SteamClient.SteamId)
        {
            transmitSeatOccupantChangeRPC(seat, NetworkManager.Singleton.LocalClientId, SteamClient.SteamId, false);
            return true;
        }
        return false;
    }

    //used for pilot, tactician, and engineers seats
    private void replaceSeatPrefab(int seat) 
    {
        if (seat == 3)
        {
            return;
        }

        if (physical_seats[seat].GetComponent<NetworkObject>() != null)
        {
            if (physical_seats[seat].GetComponent<NetworkObject>().NetworkObjectId == seat_ids[seat])
            {
                return;
            }
        }

        if (physical_seats[seat].GetComponent<NetworkObject>() == null)
        {
            GameObject.Destroy(physical_seats[seat]);
        }
        else
        {
            if (NetworkManager.Singleton.IsHost == true)
            {
                physical_seats[seat].GetComponent<NetworkObject>().Despawn(true);
            }
        }
        physical_seats[seat] = GetNetworkObject(seat_ids[seat]).gameObject;
        physical_seats[seat].GetComponent<ClientNetworkTransform>().Interpolate = true;
        physical_seats[seat].transform.GetChild(3).gameObject.SetActive(true);
    }

    public void beginShift(int seat) 
    {
        transmitShiftBeginRPC(seat);
    }

    IEnumerator adjustRetractablesHeight(float shift_time, bool up)
    {
        float starting_y_pos = captain_retractables.transform.localPosition.y;
        float dest_y_pos = 0.0f;
        if (up == false)
        {
            dest_y_pos = -0.25f;
        }

        float anim_time = shift_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            captain_retractables.transform.localPosition = new Vector3(0.0f, Mathf.Lerp(dest_y_pos, starting_y_pos,  anim_time / shift_time), captain_retractables.transform.localPosition.z);

            yield return null;
        }
    }

    IEnumerator adjustRetractablesForwardPosition(float shift_time, bool back)
    {
        float starting_z_pos = captain_retractables.transform.localPosition.z;
        float dest_z_pos = -0.32f;
        if (back == false)
        {
            dest_z_pos = 0.0f;
        }

        float anim_time = shift_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            captain_retractables.transform.localPosition = new Vector3(0.0f, captain_retractables.transform.localPosition.y, Mathf.Lerp(dest_z_pos, starting_z_pos, anim_time / shift_time));

            yield return null;
        }
    }

    IEnumerator adjustFlipoutsRotation(float rotation_time, bool enclosed)
    {
        Vector2[] rotation_values = new Vector2[] { new Vector2(0.0f, 90.0f), new Vector2(360.0f, 270.0f) };
        if (enclosed == false)
        {
            rotation_values = new Vector2[] { new Vector2(90.0f, 0.0f), new Vector2(270.0f, 360.0f) };
        }

        float anim_time = rotation_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            for (int i = 0; i < 2; i++)
            {
                captain_flipouts[i].transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, Mathf.Lerp(rotation_values[i].y, rotation_values[i].x,  anim_time / rotation_time));
            }

            yield return null;
        }
    }

    IEnumerator onCaptainSeatSitDown()
    {
        yield return StartCoroutine(adjustRetractablesHeight(0.25f, false));
        yield return StartCoroutine(adjustFlipoutsRotation(0.5f, true));
        yield return StartCoroutine(adjustRetractablesForwardPosition(0.25f, true));
        yield return StartCoroutine(adjustRetractablesHeight(0.25f, true));

        captain_seat_transformation_coroutine = null;
    }

    IEnumerator onCaptainSeatGetUp()
    {
        yield return StartCoroutine(adjustRetractablesHeight(0.25f, false));
        yield return StartCoroutine(adjustFlipoutsRotation(0.25f, false));
        yield return StartCoroutine(adjustRetractablesForwardPosition(0.25f, false));
        yield return StartCoroutine(adjustRetractablesHeight(0.5f, true));

        captain_seat_transformation_coroutine = null;
    }

    public void encloseCaptainSeat()
    {
        transmitCaptainAnimationRPC(true);
    }

    public void releaseCaptainSeat()
    {
        transmitCaptainAnimationRPC(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitCaptainAnimationRPC(bool enclose)
    {
        if (captain_seat_transformation_coroutine != null)
        {
            StopCoroutine(captain_seat_transformation_coroutine);
        }
        if (enclose == true)
        {
            captain_seat_transformation_coroutine = StartCoroutine(onCaptainSeatSitDown());
        }
        else
        {
            captain_seat_transformation_coroutine = StartCoroutine(onCaptainSeatGetUp());
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSeatOccupantChangeRPC(int seat, ulong client_id, ulong occupant_steam_id, bool occupied)
    {
        if (occupied == true)
        {
            occupied_seats[seat] = occupant_steam_id;
            player_manager.freezePlayer(occupant_steam_id);
            if (seat < 3)
            {
                if (NetworkManager.Singleton.IsHost == true)
                {
                    GameObject new_seat = GameObject.Instantiate(seat_prefabs[seat], physical_seats[3].transform.parent);
                    new_seat.transform.localPosition = new Vector3(SEAT_COORDINATES[seat][seat_indexes[seat]].x, 0.0f, SEAT_COORDINATES[seat][seat_indexes[seat]].y);
                    new_seat.GetComponent<NetworkObject>().SpawnWithOwnership(client_id, false);
                    new_seat.GetComponent<NetworkObject>().TrySetParent(physical_seats[3].transform.parent.gameObject, true);
                    transmitSeatPrefabSpawnRPC(seat, new_seat.GetComponent<NetworkObject>().NetworkObjectId);
                }
            }
        }
        else
        {
            occupied_seats[seat] = 0;
            player_manager.unfreezePlayer(occupant_steam_id);
            replaceSeatPrefab(seat);
        }
        bool[] curr_seats = new bool[4];
        for (int i = 0; i < 4; i++)
        {
            curr_seats[i] = occupied_seats[i] > 0;
        }
        power_control.updatePlayerNotifiers(seat, curr_seats);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSeatIndexChangeRPC(int seat, int new_seat_index)
    {
        seat_indexes[seat] = new_seat_index;
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSeatPrefabSpawnRPC(int seat, ulong seat_id)
    {
        seat_ids[seat] = seat_id;
    }

    [Rpc(SendTo.Everyone)]
    private void transmitShiftBeginRPC(int seat)
    {
        replaceSeatPrefab(seat);
    }
}