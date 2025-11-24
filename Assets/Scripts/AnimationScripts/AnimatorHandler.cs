/*
    AnimatorHandler.cs
    - Handles IK positioning
    - Handles animation events (sitting down, standing up)
    Contributor(s): John Aylward
    Last Updated: 11/15/2025
*/

using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorHandler : MonoBehaviour
{
    protected Animator animator;

    [SerializeField]
    private bool ikActive = true;
    [SerializeField]
    private bool ikHead = true;
    [SerializeField]
    private bool ikRightArm = false;
    [SerializeField]
    private bool ikLeftArm = false;
    [SerializeField]
    private Transform rightHandObj = null;
    [SerializeField]
    private Transform leftHandObj = null;
    [SerializeField]
    private Transform lookObj = null;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void setIKActive(bool value)
    {
        ikActive = value;
    }

    public void setIKHead(bool value)
    {
        ikHead = value;
    }

    public void setIKRightArm(bool value)
    {
        ikRightArm = value;
    }

    public void setIKLeftArm(bool value)
    {
        ikLeftArm = value;
    }

    public void setRightArmIKPosition(Vector3 pos)
    {
        rightHandObj.position = pos;
    }

    public void setLeftArmIKPosition(Vector3 pos)
    {
        leftHandObj.position = pos;
    }

    public void setHeadIKPosition(Vector3 pos)
    {
        lookObj.position = pos;
    }

    public void onSitAnimationEnd()
    {
        if (transform.parent.GetComponent<NetworkObject>().IsOwner == true)
        {
            ControlScript.Instance.assumePosition();
        }

    }

    public void onGetUpAnimationEnd()
    {
        if (transform.parent.GetComponent<NetworkObject>().IsOwner == true)
        {
            if (animator.GetInteger("Seat") == 3)
            {
                transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            }
            else
            {
                animator.applyRootMotion = false;
                animator.StopPlayback();
                int to_realign = 0;
                if (animator.GetBool("IsLeft") == true)
                {
                    to_realign = 1;
                }
                Vector3 char_position = transform.position;
                transform.parent.position = char_position + new Vector3(0.0f, -0.12f, 0.0f);
                transform.position = char_position;
            }

            StartCoroutine(playerReadjustment());
        }
    }

    IEnumerator playerReadjustment()
    {
        Vector3 this_pos = transform.localPosition;

        float anim_time = 0.15f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            transform.localPosition = Vector3.Lerp(new Vector3(0.0f, 0.12f, 0.0f), this_pos, anim_time / 10.0f);
            
            yield return null;
        }

        animator.SetBool("GettingUp", false);
        animator.SetBool("SittingDown", false);
        ControlScript.Instance.relinquishPosition();
    }

    //a callback for calculating IK
    void OnAnimatorIK()
    {
        if (!animator) return;

        //if the IK is active, set the position and rotation directly to the goal.
        if (!ikActive) return;

        if (ikHead)
        {
            // Set the look target position, if one has been assigned
            if (lookObj != null)
            {
                animator.SetLookAtWeight(1);
                animator.SetLookAtPosition(lookObj.position);
            }
        }
        else
        {
            animator.SetLookAtWeight(0);
        }

        if (ikRightArm)
        {
            // Set the right hand target position and rotation, if one has been assigned
            if (rightHandObj != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandObj.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandObj.rotation);
            }
        }
        //if the IK is not active, set the position and rotation of the hand back to the original position
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
        }


        if (ikLeftArm)
        {
            if (leftHandObj != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandObj.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandObj.rotation);
            }
        }
        //if the IK is not active, set the position and rotation of the hand back to the original position
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        }
    }
}
