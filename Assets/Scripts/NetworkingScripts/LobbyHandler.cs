/*
    LobbyHandler.cs
    - Handles RPCs that pertain to lobby functions, ex. load initiation, difficulty handling
    Contributor(s): Jake Schott
    Last Updated: 3/13/2026
*/

using UnityEngine;
using Unity.Netcode;

public class LobbyHandler : NetworkBehaviour
{
    //CLASS CONSTANTS
    public const int DEFAULT_DIFFICULTY = 0; //Easy

    private int difficulty = -1;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void updateDifficulty(int new_difficulty)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        updateDifficultyRPC(new_difficulty);
    }

    public int getDifficulty()
    {
        return difficulty;
    }

    //called by host when restarting a game or when engage is clicked
    public void startLoadForAllPlayers()
    {
        allPlayersLoadRPC(); //triggers below RPC
    }

    //called by host when change in difficulty
    [Rpc(SendTo.Everyone)]
    private void updateDifficultyRPC(int new_difficulty)
    {
        difficulty = new_difficulty;

        GameObject campaignLobby = GameObject.Find("CampaignLobby");
        if (campaignLobby != null)
        {
            campaignLobby.GetComponent<CampaignLobbyController>().DisplayDifficultyChange(new_difficulty);
        }
    }

    //only called when loading into the start of a game (there is a waiting period when the host loads into BridgeEnvironment compared to clients)
    [Rpc(SendTo.Everyone)]
    private void allPlayersLoadRPC()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            GameObject.Find("LoadHandler").GetComponent<LoadHandler>().startLoad();
        }
    }
}
