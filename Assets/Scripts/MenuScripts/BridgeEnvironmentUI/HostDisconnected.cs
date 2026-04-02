using Unity.Netcode;
using UnityEngine;

public class HostDisconnected : MonoBehaviour
{
    public GameObject HostDisconnectedScreen;

    void Start()
    {
       if (NetworkManager.Singleton != null)
       {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnHostDisconnected;
            
            Debug.Log("Subscribed");
       }
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnHostDisconnected;

            Debug.Log("Unsubscribed");
        }
    }

    private void OnHostDisconnected(ulong clientID)
    {
        Debug.Log("Host disconnected.");

        PrimaryScript.Instance.unpause(); //forces unpause  

        PrimaryScript.Instance.deactivate(false, true); //stops control interaction

        HostDisconnectedScreen.SetActive(true);

        Debug.Log("Host disconnected screen up.");
    }
    public void HandleMainMenuButtonClick()
    {
        PlayerManager.leaveGame();
    }
}
