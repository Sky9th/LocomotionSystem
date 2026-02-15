using UnityEngine;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Minimal ground contact helper for Locomotion v2.
    ///
    /// This version only performs a simple raycast straight down
    /// from the agent's position and reports whether anything was hit.
    /// Slope angles, step heights and materials will be added later
    /// as configuration grows.
    /// </summary>
    internal static class LocomotionGroundDetection
    {
        internal static SGroundContact SampleGround(Vector3 origin, float rayLength, int layerMask, float maxSlopeAngleDegrees)
        {
            if (rayLength <= 0f)
            {
                return SGroundContact.None;
            }

            origin.y += 0.1f;
            Ray ray = new Ray(origin, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, rayLength, layerMask, QueryTriggerInteraction.Ignore))
            {
                if (maxSlopeAngleDegrees > 0f)
                {
                    float slopeAngle = Vector3.Angle(hitInfo.normal, Vector3.up);
                    if (slopeAngle > maxSlopeAngleDegrees)
                    {
                        return SGroundContact.None;
                    }
                }

                return new SGroundContact(true, hitInfo.point, hitInfo.normal);
            }

            return SGroundContact.None;
        }
    }
}
