using UnityEngine;
using TMPro;
using Steamworks;

public class FriendInviteWithButton : MonoBehaviour
{
    private Friend friend;
    [SerializeField]
    private TextMeshProUGUI friendName;

    public void SetFriend(Friend f)
    {
        friend = f;
        friendName.text = friend.Name;
    }

    public void InviteFriendToLobby()
    {
        friend.InviteToGame("");
    }

    public void ChangeCanvasMode(int i)
    {
        PanelSwapper.Instance.SwitchPanel(i);
    }
}
