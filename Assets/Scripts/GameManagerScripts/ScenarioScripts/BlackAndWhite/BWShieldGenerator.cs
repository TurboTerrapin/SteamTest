/*
    BWShieldGenerator.cs
    - Used to control one of the four shield generators
    Contributor(s): Jake Schott
    Last Updated: 7/6/2026
*/

using Unity.Netcode;
using UnityEngine;

public class BWShieldGenerator : MonoBehaviour, ITorpedoTargetable
{
    private static float GENERATOR_ROTATION_SPEED = 75.0f;

    public BlackAndWhite black_and_white;

    [SerializeField]
    private float rotation_direction = 1.0f;

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }
    }

    private void Update()
    {
        transform.Rotate(0.0f, 0.0f, Time.deltaTime * GENERATOR_ROTATION_SPEED * rotation_direction);
    }

    public bool getTorpedoTargetable(IDamageable.DamageType damage_type)
    {
        return true;
    }
}
