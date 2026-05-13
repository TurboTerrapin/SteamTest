/*
    IUniversalCommunicable.cs
    - Interface for all scenarios that involve receiving a transmission from the ship
    Contributor(s): Jake Schott
    Last Updated: 5/12/2026
*/


using System.Collections.Generic;
public interface IUniversalCommunicable
{
    public bool checkTransmission(int frequency, List<int> code_indexes, List<int> code_is_numeric, int code_color);
    public void handleTransmission(int frequency, List<int> code_indexes, List<int> code_is_numeric, int code_color);
}
