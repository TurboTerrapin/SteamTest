/*
    MapItem.cs
    - Used for items that are detected on either the tactician radar or the engineer map
    Contributor(s): Jake Schott
    Last Updated: 8/23/2026
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
    private ScenarioMap.PointIconType interest_type = ScenarioMap.PointIconType.None;
    [SerializeField]
    private Color icon_color = Color.white;
    [SerializeField]
    private Sprite icon_sprite = null;

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

    public void setInterestItem(ScenarioMap.PointIconType type)
    {
        interest_type = type;
    }

    public void setColor(Color c)
    {
        icon_color = c;
    }

    public void setSprite(Sprite s)
    {
        icon_sprite = s;
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

    public ScenarioMap.PointIconType getInterestType()
    {
        return interest_type;
    }

    public Sprite getSprite()
    {
        return icon_sprite;
    }
}
