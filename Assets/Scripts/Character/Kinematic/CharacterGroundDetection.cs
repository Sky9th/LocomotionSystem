using UnityEngine;

namespace Game.Character.Kinematic
{
    internal static class CharacterGroundDetection
    {
        internal static SGroundContact EvaluateGroundContact(
            Vector3 distanceReferenceOrigin, Vector3 rayOrigin, float rayLength,
            Vector3 standProbeOrigin, Vector3 standBoxHalfExtents, float standProbeDistance,
            int layerMask, float maxSlopeAngleDegrees)
        {
            var isGrounded = TrySampleStandingByBox(standProbeOrigin, standBoxHalfExtents,
                standProbeDistance, layerMask, maxSlopeAngleDegrees,
                out var boxPoint, out var boxNormal, out var boxWalkable);

            if (TrySampleDistanceByRay(rayOrigin, rayLength, layerMask,
                    out var rayDistance, out var rayPoint, out var rayNormal))
            {
                rayDistance = Mathf.Max(0f, distanceReferenceOrigin.y - rayPoint.y);
                if (isGrounded) { rayPoint.y = boxPoint.y; rayNormal = boxNormal; }

                return new SGroundContact(isGrounded, rayDistance, boxWalkable, rayPoint, rayNormal);
            }

            return SGroundContact.None;
        }

        internal static bool IsWalkableSlope(Vector3 surfaceNormal, float maxSlopeAngleDegrees)
            => maxSlopeAngleDegrees <= 0f || Vector3.Angle(surfaceNormal, Vector3.up) <= maxSlopeAngleDegrees;

        private static bool TrySampleDistanceByRay(Vector3 origin, float rayLength, int layerMask,
            out float distance, out Vector3 point, out Vector3 normal)
        {
            distance = float.PositiveInfinity; point = Vector3.zero; normal = Vector3.up;
            if (!Physics.Raycast(new Ray(origin, Vector3.down), out var hit, rayLength, layerMask, QueryTriggerInteraction.Ignore))
                return false;
            point = hit.point; normal = hit.normal; distance = Mathf.Max(0f, hit.distance);
            return true;
        }

        private static bool TrySampleStandingByBox(Vector3 origin, Vector3 halfExtents, float castDistance,
            int layerMask, float maxSlopeAngleDegrees,
            out Vector3 point, out Vector3 normal, out bool isWalkable)
        {
            point = Vector3.zero; normal = Vector3.up; isWalkable = false;
            if (!Physics.BoxCast(origin, halfExtents, Vector3.down, out var hit,
                    Quaternion.identity, castDistance, layerMask, QueryTriggerInteraction.Ignore))
                return false;
            point = hit.point; normal = hit.normal;
            isWalkable = IsWalkableSlope(hit.normal, maxSlopeAngleDegrees);
            return true;
        }
    }
}
