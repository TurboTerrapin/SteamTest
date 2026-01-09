/*
    IControllable.cs
    - Interface for all controls
    - Used to define information retrieval and input handling
    Contributor(s): Jake Schott
    Last Updated: 5/12/2025
*/

using UnityEngine;
using System.Collections.Generic;

public interface IControllable
{
    public HUDInfo getHUDinfo(GameObject current_target);
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position);
}
