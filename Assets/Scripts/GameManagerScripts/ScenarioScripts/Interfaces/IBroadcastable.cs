/*
    IUniversalBroadcastable.cs
    - Interface for all scenarios that involve broadcasting a transmission to the ship
    Contributor(s): Jake Schott
    Last Updated: 6/29/2026
*/

public interface IBroadcastable
{
    public bool canFetchTransmission(float frequency);
    public UniversalCommunicatorCodeData fetchTransmission(float frequency);
}