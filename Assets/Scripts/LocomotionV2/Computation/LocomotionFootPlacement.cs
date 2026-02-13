using UnityEngine;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Foot placement helpers for Locomotion v2: samples left/right foot
    /// bone positions and determines which foot is currently leading
    /// along the character's forward axis.
    /// </summary>
    internal static class LocomotionFootPlacement
    {
        internal static bool EvaluateIsLeftFootOnFront(
            Animator animator,
            Transform modelRoot,
            Transform rootTransform,
            Vector3 locomotionHeading,
            bool currentValue)
        {
            if (animator == null)
            {
                return currentValue;
            }

            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            Vector3 leftFootPos = leftFoot != null ? leftFoot.position : Vector3.zero;
            Vector3 rightFootPos = rightFoot != null ? rightFoot.position : Vector3.zero;

            if (leftFootPos == Vector3.zero && rightFootPos == Vector3.zero)
            {
                return currentValue;
            }

            Transform originTransform = modelRoot != null ? modelRoot : rootTransform;
            Vector3 origin = originTransform != null ? originTransform.position : Vector3.zero;

            Vector3 bodyForwardAxis = modelRoot != null ? modelRoot.forward : (rootTransform != null ? rootTransform.forward : Vector3.forward);
            bodyForwardAxis.y = 0f;
            if (bodyForwardAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                bodyForwardAxis = locomotionHeading;
                bodyForwardAxis.y = 0f;
            }
            if (bodyForwardAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                bodyForwardAxis = Vector3.forward;
            }
            bodyForwardAxis.Normalize();

            float leftOffset = Vector3.Dot(leftFootPos - origin, bodyForwardAxis);
            float rightOffset = Vector3.Dot(rightFootPos - origin, bodyForwardAxis);

            const float epsilon = 0.005f;
            if (Mathf.Abs(leftOffset - rightOffset) <= epsilon)
            {
                return currentValue;
            }

            return leftOffset > rightOffset;
        }
    }
}
