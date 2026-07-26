/*
    ITorpedoTargetable.cs
    - Interface for scripts attached to any item that can be targeted by a torpedo
    Contributor(s): Jake Schott
    Last Updated: 6/29/2026
*/

public interface ITorpedoTargetable
{
    public bool getTorpedoTargetable(IDamageable.DamageType torpedo_type);
}