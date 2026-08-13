/*
    ManualTextureLinker.cs
    - Used for linking a texture and color to a manual image on button click
    Contributor(s): Jake Schott
    Last Updated: 8/13/2026
*/

using TMPro;
using UnityEngine;

public class ManualTextureLinker : MonoBehaviour, IManualLinker
{
    [SerializeField]
    private GameObject destination_object;
    [SerializeField]
    private Color texture_color = Color.white;
    [SerializeField]
    private Texture texture = null;

    public void setColor(Color c)
    {
        texture_color = c;
    }

    public void setTexture(Texture t)
    {
        texture = t;
    }

    public void link()
    {
        destination_object.GetComponent<UnityEngine.UI.RawImage>().texture = texture;
        destination_object.GetComponent<UnityEngine.UI.RawImage>().color = texture_color;
        foreach (Transform t in destination_object.transform)
        {
            if (t.GetComponent<UnityEngine.UI.RawImage>() != null)
            {
                t.GetComponent<UnityEngine.UI.RawImage>().color = texture_color;
            }
            else if (t.GetComponent<UnityEngine.UI.Image>() != null)
            {
                t.GetComponent<UnityEngine.UI.Image>().color = texture_color;
            }
            else if (t.GetComponent<TMP_Text>() != null)
            {
                t.GetComponent<TMP_Text>().color = texture_color;
            }
        }
        destination_object.gameObject.SetActive(true);
    }
}