/*
    CrewManifest.cs
    - Handles displaying crew member names from within the bridge
    Contributor(s): Jake Schott
    Last Updated: 6/21/2026
*/

using System.Collections.Generic;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CrewManifest : NetworkBehaviour
{
    public GameObject crew_manifest_display;

    public Dictionary<ulong, string> crew_member_names = new Dictionary<ulong, string>(); //key steam ID, value crew member name (ex. "J. KIRK")

    public void reportAsReady()
    {
        string crew_member_name = "B. SANDERS";

        if (PlayerPrefs.HasKey("CustomizeCharacterData"))
        {
            //get the JSON string we stored in PlayerPrefs
            string json = PlayerPrefs.GetString("CustomizeCharacterData");
            //convert the string back to a CustomizeCharacterData object
            CustomizeCharacterData data = JsonUtility.FromJson<CustomizeCharacterData>(json);
            crew_member_name = data.FirstName[0] + ". " + data.LastName;
            crew_member_name = crew_member_name.ToUpper();
        }

        transmitCrewMemberNameRPC(SteamClient.SteamId, crew_member_name);
    }

    public void updateCrewManifest()
    {
        GameObject lobby_handler = GameObject.Find("LobbyHandler");
        if (lobby_handler == null)
        {
            return;
        }

        List<ulong> plr_steam_ids = lobby_handler.GetComponent<LobbyHandler>().getPlayerSteamIDsInLobby();
        for (int i = 0; i < 4; i++)
        {
            if (i < plr_steam_ids.Count && crew_member_names.ContainsKey(plr_steam_ids[i]) == true)
            {
                crew_manifest_display.transform.GetChild(1).GetChild(i).GetComponent<TMP_Text>().SetText("• " + crew_member_names[plr_steam_ids[i]]);
                crew_manifest_display.transform.GetChild(1).GetChild(i).GetComponent<TMP_Text>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
            }
            else
            {
                crew_manifest_display.transform.GetChild(1).GetChild(i).GetComponent<TMP_Text>().SetText("• ------------------------");
                crew_manifest_display.transform.GetChild(1).GetChild(i).GetComponent<TMP_Text>().color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitCrewMemberNameRPC(ulong steam_id, string crew_member_name)
    {
        if (crew_member_names.ContainsKey(steam_id) == false)
        {
            crew_member_names.Add(steam_id, crew_member_name);
        }
        updateCrewManifest();
    }
}
