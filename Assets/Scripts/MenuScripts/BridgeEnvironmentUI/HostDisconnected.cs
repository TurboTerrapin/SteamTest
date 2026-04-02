using Unity.Netcode;
using UnityEngine;

public class HostDisconnected : MonoBehaviour
{
    public GameObject HostDisconnectedScreen;

    void Start()
    {
       if (NetworkManager.Singleton != null)
       {
            NetworkManager.Singleton.OnServerStopped += OnHostDisconnected;
       }
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStopped -= OnHostDisconnected;
        }
    }

    private void OnHostDisconnected(bool wasHost)
    {
        Debug.Log("Host disconnected.");

        PrimaryScript.Instance.unpause(); //forces unpause  

        PrimaryScript.Instance.deactivate(false, true); //stops control interaction

        HostDisconnectedScreen.SetActive(true);
    }
    public void HandleMainMenuButtonClick()
    {
        PlayerManager.leaveGame();
    }
}
