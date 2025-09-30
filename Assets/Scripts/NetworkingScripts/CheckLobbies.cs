using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

public class CheckLobbies : MonoBehaviour
{ 
    //Returns a list of unique lobbies that are owned by friends
    public static List<Lobby> GetPrivateLobbies()
    {
        List<Lobby> availableLobbies = new List<Lobby>();

        List<Friend> lobbyOwnerFriends = CheckFriends.GetFriendsInLobbies();

        foreach (Friend friend in lobbyOwnerFriends)
        {
            bool alreadyAdded = false;
            SteamId lobbyID = friend.GameInfo.Value.Lobby.Value.Id;
            
            for (int i = 0; i < availableLobbies.Count; i++)
            {
                if (availableLobbies[i].Id == lobbyID)
                {
                    alreadyAdded = true;
                }
            }

            Lobby toAdd = friend.GameInfo.Value.Lobby.Value;
            toAdd.Refresh();
            //Means lobby has valid data after refresh attempt
            if (toAdd.Owner.Id != 0)
            {
                if (alreadyAdded == true)
                {
                    if (toAdd.Owner.Id == friend.Id)
                    {
                        foreach (Lobby l in availableLobbies)
                        {
                            if (l.Id == toAdd.Id)
                            {
                                availableLobbies.Remove(l);
                            }
                        }
                        availableLobbies.Add(toAdd);
                    }
                }
                else
                {
                    availableLobbies.Add(toAdd);
                }
            }
        }

        return availableLobbies;
    }

    /*** OLD CODE ***/
    /*public async void RefreshPublicLobbies()
    {
        LobbyQuery query = new LobbyQuery();
        query.FilterDistanceFar();
        query.WithSlotsAvailable(1);
        lobbyList = await query.RequestAsync();
        foreach (Lobby lobby in lobbyList)
        {
            GameObject lobbyObject = Instantiate<GameObject>(publicLobbyTemplate, transform);
            lobbyObject.GetComponent<PublicLobbyJoinWithButton>().SetLobby(lobby);
            lobbyObjectList.Add(lobbyObject);
        }
    }*/
}