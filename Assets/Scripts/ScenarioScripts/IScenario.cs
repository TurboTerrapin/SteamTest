/*
    IScenario.cs
    - Interface for all scenarios
    - SCENARIO SCRIPT SHOULD ALWAYS BE ATTACHED TO A ScenarioHandler OBJECT AS THE FIRST COMPONENT AFTER NETWORK OBJECT
    Contributor(s): Jake Schott
    Last Updated: 8/28/2025
*/

public interface IScenario
{
    public string getDeathMessage();

    public void initiateScenario();
}
