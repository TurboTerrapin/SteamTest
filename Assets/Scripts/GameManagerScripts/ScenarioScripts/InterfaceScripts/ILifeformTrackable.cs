/*
    ILifeformCommunicable.cs
    - Interface for all scenarios that involve checking for lifeforms on the inside or outside of the ship
    Contributor(s): Jake Schott
    Last Updated: 8/27/2026
*/

public interface ILifeformCommunicable
{
    public bool hasLifeforms(int state);

    public LifeformScanData retrieveLifeformData(int state);
}