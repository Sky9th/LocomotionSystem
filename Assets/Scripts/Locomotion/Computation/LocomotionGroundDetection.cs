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
        private const float CastOriginVerticalOffset = 0.1f;

        private static bool TrySampleDistanceByRay(
            Vector3 origin,
            float rayLength,
            int layerMask,
            out float distanceToGround,
            out Vector3 point,
            out Vector3 normal)
        {
            distanceToGround = float.PositiveInfinity;
            point = Vector3.zero;
            normal = Vector3.up;
            if (rayLength <= 0f)
            {
                return false;
            }

            origin.y += CastOriginVerticalOffset;
            Ray ray = new Ray(origin, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hitInfo, rayLength, layerMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            point = hitInfo.point;
            normal = hitInfo.normal;
            distanceToGround = Mathf.Max(0f, hitInfo.distance - CastOriginVerticalOffset);
            return true;
        }

        /// <summary>
        /// Uses a box cast straight down to test if the character is standing on ground.
        /// This is typically more stable than a single ray on edges/uneven surfaces.
        /// Returns true when a valid (optionally walkable) ground was hit.
        /// </summary>
        private static bool TrySampleStandingByBox(
            Vector3 origin,
            Vector3 halfExtents,
            float castDistance,
            int layerMask,
            float maxSlopeAngleDegrees,
            out Vector3 point,
            out Vector3 normal,
            out bool isWalkableSlope)
        {
            point = Vector3.zero;
            normal = Vector3.up;
            isWalkableSlope = false;
            if (castDistance <= 0f)
            {
                return false;
            }

            if (halfExtents.x <= 0f || halfExtents.y <= 0f || halfExtents.z <= 0f)
            {
                return false;
            }

            origin.y += CastOriginVerticalOffset;
            if (!Physics.BoxCast(
                    origin,
                    halfExtents,
                    Vector3.down,
                    out RaycastHit hitInfo,
                    Quaternion.identity,
                    castDistance,
                    layerMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            point = hitInfo.point;
            normal = hitInfo.normal;
            isWalkableSlope = IsWalkableSlope(hitInfo.normal, maxSlopeAngleDegrees);
            return true;
        }

        /// <summary>
        /// Evaluates ground data by combining:
        /// - a ray (distance-to-ground, potentially longer)
        /// - a box cast (standing-on-ground state, more stable)
        /// </summary>
        internal static SGroundContact EvaluateGroundContact(
            Vector3 origin,
            float distanceRayLength,
            Vector3 standBoxHalfExtents,
            float standBoxCastDistance,
            int layerMask,
            float maxSlopeAngleDegrees)
        {
            // 1) Standing test first (stable contact). If standing, evaluate slope.
            bool isStanding = TrySampleStandingByBox(
                origin,
                standBoxHalfExtents,
                standBoxCastDistance,
                layerMask,
                maxSlopeAngleDegrees,
                out Vector3 boxPoint,
                out Vector3 boxNormal,
                out bool boxWalkable);

            if (isStanding)
            {
                // When standing, distance-to-ground should be treated as 0.
                // (If callers want a more detailed separation value later, we can
                // add an optional "standing separation" output separately.)
                return new SGroundContact(
                    isGrounded: true,
                    distanceToGround: 0f,
                    isWalkableSlope: boxWalkable,
                    point: boxPoint,
                    normal: boxNormal);
            }

            // 2) If not standing, measure distance to ground via ray.
                if (!TrySampleDistanceByRay(
                    origin,
                    distanceRayLength,
                    layerMask,
                    out float rayDistance,
                    out Vector3 rayPoint,
                    out Vector3 rayNormal))
            {
                return SGroundContact.None;
            }

            return new SGroundContact(
                isGrounded: false,
                distanceToGround: rayDistance,
                isWalkableSlope: false,
                point: rayPoint,
                normal: rayNormal);
        }

        /// <summary>
        /// Returns true if a surface with the given normal should be
        /// considered walkable for the specified maximum slope angle.
        /// </summary>
        internal static bool IsWalkableSlope(Vector3 surfaceNormal, float maxSlopeAngleDegrees)
        {
            // Treat non-positive as "no slope limit".
            if (maxSlopeAngleDegrees <= 0f)
            {
                return true;
            }

            float slopeAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            return slopeAngle <= maxSlopeAngleDegrees;
        }

    }
}
