/*
    MapItem.cs
    - Used for items that are detected on either the tactician radar or the engineer map
    Contributor(s): Jake Schott
    Last Updated: 3/13/2026
*/

using UnityEngine;

public class MapItem : MonoBehaviour
{
    [SerializeField]
    private float size = 1.0f;
    [SerializeField]
    private bool is_ship = false;
    [SerializeField]
    private bool is_visible = false;
    [SerializeField]
    private bool is_interest_item = false;
    [SerializeField]
    private Color icon_color = Color.white;
    [SerializeField]
    private Texture icon_texture = null;

    public void setVisibility(bool v)
    {
        is_visible = v;
    }

    public void setSize(int s)
    {
        size = s;
    }

    public void setShip(bool ship)
    {
        is_ship = ship;
    }

    public void setInterestItem(bool interest)
    {
        is_interest_item = interest;
    }

    public void setColor(Color c)
    {
        icon_color = c;
    }

    public void setTexture(Texture t)
    {
        icon_texture = t;
    }

    public float getSize()
    {
        return size;
    }

    public Color getColor()
    {
        return icon_color;
    }

    public bool isShip()
    {
        return is_ship; 
    }

    public bool isVisible()
    {
        return is_visible;
    }

    public bool isInterestItem()
    {
        return is_interest_item;
    }

    public Texture getTexture()
    {
        return icon_texture;
    }
}
