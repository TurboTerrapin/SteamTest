/*
    BWWall.cs
    - Used to stun the ship if it flies into a collider while active
    Contributor(s): Jake Schott
    Last Updated: 7/18/2026
*/

using Unity.Netcode;
using UnityEngine;

public class BWWall : MonoBehaviour
{
    public BlackAndWhite black_and_white;

    private bool is_active = false;

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }
    }

    public void activate()
    {
        is_active = true;
    }

    public void deactivate()
    {
        is_active = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (is_active == false)
        {
            return;
        }

        if (other.gameObject.layer == 9) //stun ship
        {
            black_and_white.shipEnteredBarrier();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (is_active == false)
        {
            return;
        }

        if (collision.collider.gameObject.layer == 9) //stun ship
        {
            black_and_white.shipEnteredBarrier();
        }
    }
}
