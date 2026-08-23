using UnityEngine;
using TMPro;
using Steamworks.Data;

public class FriendJoinWithButton : MonoBehaviour
{
    private Lobby lobby;
    [SerializeField]
    private TextMeshProUGUI ownerName;
    [SerializeField]
    private TextMeshProUGUI players;
    [SerializeField]
    private GameObject joinButton;

    private JoinCampaignMenuController joinCampaignMenuController;

    private void DeactivateJoinButton()
    {
        //Make button uninteractable
        joinButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
        //Fade button
        UnityEngine.Color buttonColor = joinButton.GetComponent<UnityEngine.UI.Image>().color;
        joinButton.GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.2f);
        //Fade button text (JOIN)
        joinButton.transform.GetChild(1).GetComponent<TMP_Text>().color = new UnityEngine.Color(1.0f, 1.0f, 1.0f, 0.2f);
        //Fade button border
        UnityEngine.Color borderColor = joinButton.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color;
        joinButton.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(borderColor.r, borderColor.g, borderColor.b, 0.2f);
    }

    public void SetLobby(Lobby l, JoinCampaignMenuController jcmc)
    {
        lobby = l;
        joinCampaignMenuController = jcmc;
        ownerName.text = l.Owner.Name;
        players.text = GetPlayers().ToString() + "/4";
        if (GetPlayers() >= 4)
        {
            DeactivateJoinButton();
        }
    }

    public void JoinFriendLobby()
    {
        if (GetPlayers() < 4)
        {
            joinCampaignMenuController.StopCheckingLobbies();
            GameNetworkManager.Instance.JoinUsingButton(lobby);
        }
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