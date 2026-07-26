/*
    ExplosionTransmitter.cs
    - Just used to separate Explosion.cs from NetworkBehaviour so you can have explosions on non-network objects
    Contributor(s): Jake Schott
    Last Updated: 6/29/2026
*/

using Unity.Netcode;
using UnityEngine;

public class ExplosionTransmitter : NetworkBehaviour
{

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC(bool vo)
    {
        GetComponent<Explosion>().explode(vo);
    }

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC(float s, bool vo)
    {
        GetComponent<Explosion>().explode(s, vo);
    }

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC(float s, bool vo, Color c)
    {
        GetComponent<Explosion>().explode(s, vo, c);
    }

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC(float s, bool vo, Color b, Color a)
    {
        GetComponent<Explosion>().explode(s, vo, b, a);
    }
}
