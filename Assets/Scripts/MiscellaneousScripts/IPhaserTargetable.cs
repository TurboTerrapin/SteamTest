/*
    IPhaserTargetable.cs
    - Interface for scripts attached to any item that can be targeted by phasers
    Contributor(s): Jake Schott
    Last Updated: 7/6/2026
*/

public interface IPhaserTargetable
{
    public bool getPhaserTargetable(IDamageable.DamageType phaser_type);
}