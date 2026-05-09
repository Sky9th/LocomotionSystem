using UnityEngine;

namespace Game.Character.Kinematic
{
    internal static class CharacterGroundDetection
    {
        internal static SGroundContact EvaluateGroundContact(
            Vector3 position, float probeHeight, float probeRadius,
            int layerMask, float maxSlopeAngleDegrees)
        {
            var origin = position + Vector3.up * probeHeight;
            var maxDistance = probeHeight + 10f;

            if (Physics.SphereCast(origin, probeRadius, Vector3.down, out var hit,
                    maxDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
                var distanceToGround = Mathf.Max(0f, hit.distance - probeHeight);
                var isGrounded = distanceToGround < 0.15f;
                var isWalkable = isGrounded && IsWalkableSlope(hit.normal, maxSlopeAngleDegrees);
                var contactPoint = hit.point;
                return new SGroundContact(isGrounded, distanceToGround, isWalkable, contactPoint, hit.normal);
            }

            return SGroundContact.None;
        }

        internal static bool IsWalkableSlope(Vector3 surfaceNormal, float maxSlopeAngleDegrees)
            => maxSlopeAngleDegrees <= 0f || Vector3.Angle(surfaceNormal, Vector3.up) <= maxSlopeAngleDegrees;
    }
}
