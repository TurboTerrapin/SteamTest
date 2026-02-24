/*
    IControllable.cs
    - Interface for all controls
    - Used to define information retrieval and input handling
    Contributor(s): Jake Schott
    Last Updated: 5/12/2025
*/

using UnityEngine;
using System.Collections.Generic;
public interface IIKTargetable
{
    public Transform getIKTarget(GameObject current_target);
    public AnimatorHandler.HandInteractionType getHandInteractionType();
}
