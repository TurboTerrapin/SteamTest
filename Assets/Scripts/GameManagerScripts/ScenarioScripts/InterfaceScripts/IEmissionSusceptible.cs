/*
    IEmissionSusceptible.cs
    - Interface for all scenarios that are affected by emission reducers
    Contributor(s): Jake Schott
    Last Updated: 6/25/2026
*/


public interface IEmissionSusceptible
{
    public void onEmissionChange();
}