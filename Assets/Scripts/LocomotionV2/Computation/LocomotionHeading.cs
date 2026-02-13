using UnityEngine;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Helper methods for deriving the planar locomotion heading
    /// from the follow anchor or root transform.
    /// </summary>
    internal static class LocomotionHeading
    {
        internal static Vector3 Evaluate(Transform followAnchor, Transform rootTransform)
        {
            Transform source = followAnchor != null ? followAnchor : rootTransform;
            if (source == null)
            {
                return Vector3.forward;
            }

            Vector3 forward = source.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }
    }
}
