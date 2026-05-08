using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Character.Probes
{
    /// <summary>
    /// Minimal ground contact helper for Locomotion v2.
    ///
    /// This version only performs a simple raycast straight down
    /// from the agent's position and reports whether anything was hit.
    /// Slope angles, step heights and materials will be added later
    /// as configuration grows.
    /// </summary>
    internal static class CharacterGroundDetection
    {

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

            Ray ray = new Ray(origin, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hitInfo, rayLength, layerMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            point = hitInfo.point;
            normal = hitInfo.normal;
            distanceToGround = Mathf.Max(0f, hitInfo.distance);
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
            Vector3 distanceReferenceOrigin,
            Vector3 rayOrigin,
            float rayLength,
            Vector3 standProbeOrigin,
            Vector3 standBoxHalfExtents,
            float standProbeDistance,
            int layerMask,
            float maxSlopeAngleDegrees)
        {
            SGroundContact groundContact;

            // 1) Standing test first (stable contact). If standing, evaluate slope.
            bool isGrounded = TrySampleStandingByBox(
                standProbeOrigin,
                standBoxHalfExtents,
                standProbeDistance,
                layerMask,
                maxSlopeAngleDegrees,
                out Vector3 boxPoint,
                out Vector3 boxNormal,
                out bool boxWalkable);

            if (TrySampleDistanceByRay(
                rayOrigin,
                rayLength,
                layerMask,
                out float rayDistance,
                out Vector3 rayPoint,
                out Vector3 rayNormal))
            {
                rayDistance = Mathf.Max(0f, distanceReferenceOrigin.y - rayPoint.y);

                if (isGrounded)
                {
                    rayPoint.y = boxPoint.y;
                    rayNormal = boxNormal;
                }

                groundContact = new SGroundContact(
                    isGrounded: isGrounded,
                    distanceToGround: rayDistance,
                    isWalkableSlope: boxWalkable,
                    point: rayPoint,
                    normal: rayNormal);
            }
            else
            {
                groundContact = SGroundContact.None;
            }

            return groundContact;
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
