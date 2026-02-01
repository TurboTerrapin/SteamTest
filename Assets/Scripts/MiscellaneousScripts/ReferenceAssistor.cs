/*
    ReferenceAssistor.cs
    - Used to streamline referencing for certain commonly-used things
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections.Generic;
using UnityEngine;

public class ReferenceAssistor : MonoBehaviour
{
    public Material lit_neon;
    public Material unlit_neon;
    public Material lit_red;
    public Material unlit_red;
    public Material lit_green;
    public Material unlit_green;
    public Material lit_purple;
    public Material unlit_purple;

    public List<GameObject> module_handlers;

    public PowerManager power_manager;

    public static ReferenceAssistor Instance { get; private set; }

    private void Awake()
    {
        //make an instance so can be referenced
        if (Instance != null)
        {
            Destroy(this);
        }
        Instance = this;
    }
}
