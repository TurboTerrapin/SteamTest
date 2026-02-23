/*
    PatternData.cs
    - Holds information about a specific energy pattern (ex. colors, number of rings, shapes, etc.)
    Contributor(s): Jake Schott
    Last Updated: 2/21/2026
*/

using System.Collections.Generic;

public class PatternData
{
    //center
    private int center_texture;
    private int center_color;
    private float center_speed;

    //rings
    private int number_of_rings = 0;
    private List<int> ring_colors = null;
    private List<int> ring_textures = null;
    private List<bool> ring_is_dotted = null;
    private List<float> ring_speeds = null;

    public void setCenter(int c, int c_color, float speed)
    {
        center_texture = c;
        center_color = c_color;
        center_speed = speed;
    }

    public void setCenterColor(int c_color)
    {
        center_color = c_color;
    }

    public void setRings(int num, List<int> colors, List<int> textures, List<bool> is_dotted, List<float> speeds)
    {
        number_of_rings = num;
        ring_colors = colors;
        ring_textures = textures;
        ring_speeds = speeds;
        ring_is_dotted = is_dotted;
    }

    public void setRings(int num, int[] colors, int[] textures, bool[] is_dotted, float[] speeds)
    {
        number_of_rings = num;
        List<int> rc = new List<int>();
        List<int> rt = new List<int>();
        List<bool> rid = new List<bool>();
        List<float> s = new List<float>();
        for (int i = 0; i < num; i++)
        {
            rc.Add(colors[i]);
            rt.Add(textures[i]);
            rid.Add(is_dotted[i]);
            s.Add(speeds[i]);
        }
        ring_colors = rc;
        ring_textures = rt;
        ring_is_dotted = rid;
        ring_speeds = s;
    }

    public void setRingColors(List<int> colors)
    {
        ring_colors = colors;
    }

    public int getCenterTexture()
    {
        return center_texture;
    }

    public int getCenterColor()
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

    public List<int> getRingColors()
    {
        return ring_colors;
    }

    public List<int> getRingTextures()
    {
        return ring_textures;
    }

    public List<bool> getRingIsDotted()
    {
        return ring_is_dotted;
    }

    public List<float> getRingSpeeds()
    {
        return ring_speeds;
    }
}