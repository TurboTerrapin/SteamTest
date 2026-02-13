/*
    PatternData.cs
    - Holds information about a specific energy pattern (ex. colors, number of rings, shapes, etc.)
    Contributor(s): Jake Schott
    Last Updated: 8/16/2025
*/

using System.Collections.Generic;
using UnityEngine;

public class PatternData
{
    private Texture center;
    private Color center_color;
    private float center_speed;

    private int number_of_rings = 0;
    private List<Color> ring_colors = null;
    private List<Texture> ring_textures = null;
    private List<bool> ring_is_solid = null;
    private List<float> ring_speeds = null;

    public void setCenter(Texture c, Color c_color, float speed)
    {
        center = c;
        center_color = c_color;
        center_speed = speed;
    }

    public void setCenterColor(Color c_color)
    {
        center_color = c_color;
    }

    public void setRings(int num, List<Color> colors, List<Texture> textures, List<bool> is_solid, List<float> speeds)
    {
        number_of_rings = num;
        ring_colors = colors;
        ring_textures = textures;
        ring_speeds = speeds;
        ring_is_solid = is_solid;
    }

    public void setRingColors(List<Color> colors)
    {
        ring_colors = colors;
    }

    public Texture getCenterTexture()
    {
        return center;
    }

    public Color getCenterColor()
    {
        return center_color;
    }

    public float getCenterSpeed()
    {
        return center_speed;
    }

    public int getNumberOfRings()
    {
        return number_of_rings;
    }

    public List<Color> getRingColors()
    {
        return ring_colors;
    }

    public List<Texture> getRingTextures()
    {
        return ring_textures;
    }

    public List<bool> getRingSolids()
    {
        return ring_is_solid;
    }

    public List<float> getRingSpeeds()
    {
        return ring_speeds;
    }
}
