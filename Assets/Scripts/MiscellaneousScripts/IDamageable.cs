/*
    IDamageable.cs
    - Interface for damageable items
    Contributor(s): Jake Schott
    Last Updated: 6/26/2026
*/

public interface IDamageable
{
    //different damage designations
    public enum DamageType
    {
        PhotonTorpedo,
        ProtonTorpedo,
        IonTorpedo,
        QuantumTorpedo,
        SuperluminalTorpedo,
        ChronitonTorpedo,
        Explosive,
        Collision,
        EnemyPhaser,
        ShortRangePhaser,
        LongRangePhaser
    }

    public void damage(float damage, DamageType damage_type);
}
