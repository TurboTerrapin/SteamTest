/*
    CollisionForwarder.cs
    - Forwards collision events to CollisionHandler
    Contributor(s): Henryk Musial
    Last Updated: 1/23/2026
*/

using UnityEngine;

public class CollisionForwarder : MonoBehaviour
{
    public CollisionHandler collisionHandler;

    private void OnCollisionEnter(Collision collision)
    {
        collisionHandler.HandleCollision(GetComponent<Collider>(), collision.collider);
    }
}