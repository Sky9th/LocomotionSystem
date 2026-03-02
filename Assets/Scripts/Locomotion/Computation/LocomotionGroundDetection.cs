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
        /// <summary>
        /// Casts a ray straight down to find ground beneath the
        /// given origin and returns a filtered contact based on the
        /// allowed slope angle.
        /// </summary>
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
                if (maxSlopeAngleDegrees > 0f &&
                    !IsWalkableSlope(hitInfo.normal, maxSlopeAngleDegrees))
                {
                    return SGroundContact.None;
                }

                return new SGroundContact(true, hitInfo.point, hitInfo.normal);
            }

            return SGroundContact.None;
        }

        /// <summary>
        /// Returns true if a surface with the given normal should be
        /// considered walkable for the specified maximum slope angle.
        /// </summary>
        internal static bool IsWalkableSlope(Vector3 surfaceNormal, float maxSlopeAngleDegrees)
        {
            if (maxSlopeAngleDegrees <= 0f)
            {
                return false;
            }

            float slopeAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            return slopeAngle <= maxSlopeAngleDegrees;
        }

        /// <summary>
        /// Convenience overload which checks whether an existing
        /// ground contact represents a walkable slope.
        /// </summary>
        internal static bool IsWalkableSlope(in SGroundContact groundContact, float maxSlopeAngleDegrees)
        {
            if (!groundContact.IsGrounded)
            {
                return false;
            }

            return IsWalkableSlope(groundContact.ContactNormal, maxSlopeAngleDegrees);
        }
    }
}
