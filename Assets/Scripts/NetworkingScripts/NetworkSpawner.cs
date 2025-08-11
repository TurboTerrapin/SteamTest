using UnityEngine;
using Unity.Netcode;

public class NetworkSpawner : MonoBehaviour
{
    public static NetworkSpawner Instance { get; private set; }

    public GameObject pref = null;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.H))
        {
            SpawnObjectServerRPC(pref);
        }
    }

    [ServerRpc]
    public void SpawnObjectServerRPC(GameObject obj)
    {
        SpawnObject(obj);
    }

    public void SpawnObject(GameObject obj)
    {
        if(obj == null)
        {
            return;
        }
        if (obj.GetComponent<NetworkObject>())
        {
            if (NetworkManager.Singleton.IsServer)
            {
                GameObject spawned = Instantiate<GameObject>(obj, Vector3.zero, Quaternion.identity);
                spawned.GetComponent<NetworkObject>().Spawn();
                return;
            }
        }
        Instantiate<GameObject>(obj, Vector3.zero, Quaternion.identity);
    }

    public void SpawnObjectAtLocation(GameObject obj, Vector3 location)
    {
        if (obj == null)
        {
            return;
        }
        if (obj.GetComponent<NetworkObject>())
        {
            if (NetworkManager.Singleton.IsServer)
            {
                GameObject spawned = Instantiate<GameObject>(obj, location, Quaternion.identity);
                spawned.GetComponent<NetworkObject>().Spawn();
                return;
            }
        }
        Instantiate<GameObject>(obj, location, Quaternion.identity);
    }
}
