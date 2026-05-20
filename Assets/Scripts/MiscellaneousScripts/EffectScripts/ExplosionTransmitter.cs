/*
    ExplosionTransmitter.cs
    - Just used to separate Explosion.cs from NetworkBehaviour so you can have explosions on non-network objects
    Contributor(s): Jake Schott
    Last Updated: 5/19/2026
*/

using Unity.Netcode;
using UnityEngine;

public class ExplosionTransmitter : NetworkBehaviour
{

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC()
    {
        GetComponent<Explosion>().explode();
    }

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC(float s)
    {
        GetComponent<Explosion>().explode(s);
    }

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC(float s, Color c)
    {
        GetComponent<Explosion>().explode(s, c);
    }

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC(float s, Color b, Color a)
    {
        GetComponent<Explosion>().explode(s, b, a);
    }
}
