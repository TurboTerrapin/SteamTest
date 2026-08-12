/*
    IScenario.cs
    - Interface for all scenarios
    - SCENARIO SCRIPT SHOULD ALWAYS BE ATTACHED TO A ScenarioHandler OBJECT
    Contributor(s): Jake Schott
    Last Updated: 7/6/2026
*/

public interface IScenario
{
    public string getDeathMessage();

    public void prepScenario();

    public void initiateScenario();
}
