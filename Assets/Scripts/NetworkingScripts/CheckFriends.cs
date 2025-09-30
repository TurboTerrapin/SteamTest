using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class CheckFriends : MonoBehaviour
{
    //Returns a list of all online friends
    public static List<Friend> GetOnlineFriends()
    {
        List<Friend> onlineFriends = new List<Friend>();

        //Check all your friends
        foreach (Friend friend in SteamFriends.GetFriends())
        {
            //If they're online
            if (friend.IsOnline)
            {
                //Add to the list
                onlineFriends.Add(friend);
            }
        }
        return onlineFriends;
    }

    //Returns a list of friends who are lobby owners
    public static List<Friend> GetFriendsInLobbies()
    {
        List<Friend> lobbyFriends = new List<Friend>();

        //Check all your friends
        foreach (Friend friend in SteamFriends.GetFriends())
        {
            if (friend.IsOnline && friend.GameInfo.HasValue)
            {
                //If they're in a Deep Space Five lobby 
                if (friend.IsPlayingThisGame && friend.GameInfo.Value.Lobby.HasValue)
                {
                    //Add to the list
                    lobbyFriends.Add(friend);
                }
            }
        }

        return lobbyFriends;
    }

    //Returns a list of all online friends who are not in a Deep Space Five lobby
    public static List<Friend> GetOnlineFriendsNotInAnyLobby()
    {
        List<Friend> onlineFriendsNotInLobby = new List<Friend>();

        //Make sure there is a lobby to check against in the first place
        if (GameNetworkManager.Instance.currentLobby.HasValue == false)
        {
            return onlineFriendsNotInLobby;
        }

        //Check all your friends
        foreach (Friend friend in SteamFriends.GetFriends())
        {
            if (friend.IsOnline && friend.GameInfo.HasValue)
            {
                //If they're not in a Deep Space Five lobby 
                if (!(friend.IsPlayingThisGame && friend.GameInfo.Value.Lobby.HasValue))
                {
                    //Add to the list
                    onlineFriendsNotInLobby.Add(friend);
                }
            }
        }
        return onlineFriendsNotInLobby;
    }

    //Returns a list of all friends playing Deep Space Five
    public static List<Friend> GetFriendsInGame()
    {
        List<Friend> friendsInGame = new List<Friend>();

        foreach (Friend friend in SteamFriends.GetFriends())
        {
            //If they're playing the game
            if (friend.IsOnline && friend.IsPlayingThisGame)
            {
                //Add to the list
                friendsInGame.Add(friend);
            }
        }
        return friendsInGame;
    }

    //Returns a list of all online friends who are in the current lobby
    public static List<Friend> GetFriendsInSameLobby()
    {
        List<Friend> friendsInLobby = new List<Friend>();

        //Make sure there is a lobby to check against in the first place
        if (GameNetworkManager.Instance.currentLobby.HasValue == false)
        {
            return friendsInLobby;
        }

        //Check all your friends
        foreach (Friend friend in SteamFriends.GetFriends())
        {
            if (friend.IsOnline && friend.GameInfo.HasValue)
            {
                //If they're in a lobby in this game
                if (friend.IsPlayingThisGame && friend.GameInfo.Value.Lobby.HasValue)
                {
                    //If they're in the same lobby
                    if (friend.GameInfo.Value.Lobby.Value.Id == GameNetworkManager.Instance.currentLobby.Value.Id)
                    {
                        //Add to the list
                        friendsInLobby.Add(friend);
                    }
                }
            }
        }
        return friendsInLobby;
    }
}