/*
    IUniversalBroadcastable.cs
    - Interface for all scenarios that involve broadcasting a transmission to the ship
    Contributor(s): Jake Schott
    Last Updated: 7/31/2025
*/

public interface IBroadcastable
{
    public bool canFetchTransmission(int frequency);
    public void fetchTransmission(int frequency);
}