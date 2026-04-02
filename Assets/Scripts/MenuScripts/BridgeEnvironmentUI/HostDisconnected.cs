using Unity.Netcode;
using UnityEngine;

public class HostDisconnected : MonoBehaviour
{
    public GameObject HostDisconnectedScreen;
    public GameObject FailureHandlerCamera;

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
        Debug.Log(clientID);

        if (clientID != NetworkManager.Singleton.LocalClientId) 
        {
            return;
        }

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
