/*
    ITractorBeamInfo.cs
    - Interface for scripts attached to any item that can be sucked in by the tractor beam
    - Items who implement this interface are not inherently collectible
    Contributor(s): Jake Schott
    Last Updated: 8/23/2026
*/

using UnityEngine;

public interface ITractorBeamable
{
    public bool getTractorBeamable();

    public Texture getItemTexture();

    public Color getItemColor();

    public string getSerialNumber();
}