/*
    IUniversalCommunicable.cs
    - Interface for all scenarios that involve receiving a transmission from the ship
    Contributor(s): Jake Schott
    Last Updated: 7/31/2025
*/


using System.Collections.Generic;
public interface IUniversalCommunicable
{
    public bool checkTransmission(int frequency, List<int> code_indexes, List<int> code_colors, List<int> code_is_numeric);
    public void handleTransmission(int frequency, List<int> code_indexes, List<int> code_colors, List<int> code_is_numeric);
}
