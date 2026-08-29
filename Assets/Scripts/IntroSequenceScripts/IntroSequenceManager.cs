/*
    IntroSequenceManager.cs
    - Used to manage the intro sequence where the player walks around (tutorial)
    Contributor(s): Jake Schott
    Last Updated: 8/29/2026
*/

using UnityEngine;

public class IntroSequenceManager : MonoBehaviour
{
    public GameObject player;
    
    private void Start()
    {
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().endLoad(true);
        player.GetComponent<CameraMove>().GetCamera().SetActive(true);
        PrimaryScript.Instance.unlockPlayer(player);
    }
}