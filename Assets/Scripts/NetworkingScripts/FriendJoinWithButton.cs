using UnityEngine;
using TMPro;
using Steamworks.Data;
using Unity.Netcode;


public class FriendJoinWithButton : MonoBehaviour
{
    [SerializeField]
    private Lobby lobby;
    [SerializeField]
    private TextMeshProUGUI friendName;
    [SerializeField]
    private TextMeshProUGUI players;

    public void SetLobby(Lobby l)
    {
        lobby = l;
        friendName.text = l.Owner.Name;
        players.text = GetPlayers().ToString() + "/4";
    }

    public void JoinFriendLobby()
    {
        GameNetworkManager.Instance.JoinWithButton(lobby);
        //used for loading
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().connectNetworkManager();
    }

    public void ChangeCanvasMode(int i)
    {
        PanelSwapper.Instance.SwitchPanel(i);
    }

    private int GetPlayers()
    {
        return Mathf.Max(1, lobby.MemberCount);
    }
}
