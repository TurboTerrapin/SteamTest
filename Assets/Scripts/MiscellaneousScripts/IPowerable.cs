/*
    IPowerable.cs
    - Interface for controls/sensors that can be powered on/off
    Contributor(s): Jake Schott
    Last Updated: 8/19/2025
*/

public interface IPowerable
{
    public void powerOn(int position);

    public void powerOff(int position, float time);
}