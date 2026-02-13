/*
    ITractorBeamInfo.cs
    - Interface for scripts attached to any item that can be sucked in by the tractor beam
    - Items who implement this interface are not inherently collectible
    Contributor(s): Jake Schott
    Last Updated: 1/30/2026
*/

using UnityEngine;

public interface ITractorBeamable
{
    public Texture getItemTexture();

    public Color getItemColor();
}
