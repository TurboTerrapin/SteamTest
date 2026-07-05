/*
    BlackAndWhiteEmitter.cs
    - Used to control one of the six radiation emitters behind the wall
    Contributor(s): Jake Schott
    Last Updated: 7/4/2026
*/

using Unity.Netcode;
using UnityEngine;

public class BlackAndWhiteEmitter : MonoBehaviour
{
    private static float EMITTER_ROTATION_SPEED = 25.0f;
    private static float RADIATION_ROTATION_SPEED = 150.0f;

    public BlackAndWhite black_and_white;

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }
        transform.Rotate(0.0f, 0.0f, Random.Range(0.0f, 180.0f));
        transform.GetChild(0).Rotate(0.0f, 0.0f, Random.Range(0.0f, 180.0f));
    }

    private void Update()
    {
        transform.Rotate(0.0f, 0.0f, Time.deltaTime * EMITTER_ROTATION_SPEED);
        transform.GetChild(0).Rotate(0.0f, 0.0f, Time.deltaTime * RADIATION_ROTATION_SPEED);
    }
}
