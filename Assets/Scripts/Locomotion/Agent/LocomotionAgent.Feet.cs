using UnityEngine;

/// <summary>
/// Foot placement helpers for LocomotionAgent: sampling left/right foot positions
/// and determining which foot is currently leading along the character's forward axis.
/// </summary>
public partial class LocomotionAgent : MonoBehaviour
{

    private Vector3 GetLeftFootWorldPosition()
    {
        if (animator == null)
        {
            return Vector3.zero;
        }

        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        return leftFoot != null ? leftFoot.position : Vector3.zero;
    }

    private Vector3 GetRightFootWorldPosition()
    {
        if (animator == null)
        {
            return Vector3.zero;
        }

        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        return rightFoot != null ? rightFoot.position : Vector3.zero;
    }

    private void UpdateFootFrontState()
    {
        Vector3 leftFootPos = GetLeftFootWorldPosition();
        Vector3 rightFootPos = GetRightFootWorldPosition();

        if (leftFootPos == Vector3.zero && rightFootPos == Vector3.zero)
        {
            return;
        }

        Vector3 origin = modelRoot != null ? modelRoot.position : transform.position;

        Vector3 forward = modelRoot != null ? modelRoot.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= Mathf.Epsilon)
        {
            forward = forwardDirection;
            forward.y = 0f;
        }
        if (forward.sqrMagnitude <= Mathf.Epsilon)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        float leftOffset = Vector3.Dot(leftFootPos - origin, forward);
        float rightOffset = Vector3.Dot(rightFootPos - origin, forward);

        const float epsilon = 0.005f;
        if (Mathf.Abs(leftOffset - rightOffset) <= epsilon)
        {
            return;
        }

        isLeftFootOnFront = leftOffset > rightOffset;
    }
}
