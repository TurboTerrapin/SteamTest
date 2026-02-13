/*
    IDescribable.cs
    - Interface for all non-interactable items
    - Used to define information retrieval for things like partial prefix code or power consumption
    - Just IControllable but without the inputs
    Contributor(s): Jake Schott
    Last Updated: 1/4/2026
*/

using UnityEngine;

public interface IDescribable
{
    public HUDInfo getHUDinfo(GameObject current_target);
}
