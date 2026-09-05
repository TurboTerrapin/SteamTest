/*
    IIKTargetable.cs
    - Interface for all controls that are interactable by the player
    - Used to define information retrieval for Inverse Kinematics
    Contributor(s): John Aylward
    Last Updated: 4/16/2026
*/

using UnityEngine;
using System.Collections.Generic;
public interface IIKTargetable
{
    public Transform getIKTarget(GameObject current_target);
    public AnimatorHandler.HandInteractionType getHandInteractionType();
    public float getHandPose();
    public bool getRightHandFlip();
    public Vector3 getRightHandOffset();
    public float getLerpSpeed();
    //public void setRightHandSpecificTargets(bool value);
}
