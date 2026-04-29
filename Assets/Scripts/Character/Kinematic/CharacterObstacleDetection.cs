using UnityEngine;

namespace Game.Character.Kinematic
{
    internal static class CharacterObstacleDetection
    {
        internal static bool TryDetectForwardObstacle(
            Vector3 actorPosition, Vector3 forward, float probeVerticalOffset,
            float probeDistance, int layerMask, float maxClimbHeight, float maxSlopeAngleDegrees,
            out SForwardObstacleDetection result)
        {
            result = SForwardObstacleDetection.None;

            var origin = actorPosition + Vector3.up * Mathf.Max(0f, probeVerticalOffset);
            if (!Physics.Raycast(origin, forward, out var hit, probeDistance, layerMask, QueryTriggerInteraction.Ignore))
                return false;

            var surfaceAngle = Vector3.Angle(hit.normal, Vector3.up);
            var isSlope = surfaceAngle <= maxSlopeAngleDegrees;
            var isObstacle = !isSlope;

            bool hasTopSurface = false;
            var topPoint = Vector3.zero;
            var topNormal = Vector3.up;

            if (isObstacle)
            {
                var topOrigin = hit.point + forward * 0.05f;
                topOrigin.y = actorPosition.y + maxClimbHeight + 0.05f;
                if (Physics.Raycast(topOrigin, Vector3.down, out var topHit, maxClimbHeight * 2f, layerMask, QueryTriggerInteraction.Ignore))
                {
                    hasTopSurface = true;
                    topPoint = topHit.point;
                    topNormal = topHit.normal;
                }
            }

            var obstacleHeight = hasTopSurface ? Mathf.Max(0f, topPoint.y - hit.point.y) : float.PositiveInfinity;
            var canClimb = isObstacle && hasTopSurface && obstacleHeight <= maxClimbHeight;

            result = new SForwardObstacleDetection(
                hasHit: true,
                hasTopSurface: hasTopSurface,
                isSlope: isSlope,
                isObstacle: isObstacle,
                canClimb: canClimb,
                canVault: false,
                canStepOver: false,
                distance: hit.distance,
                obstacleHeight: obstacleHeight,
                point: hit.point,
                normal: hit.normal,
                topPoint: topPoint,
                topNormal: topNormal,
                surfaceAngle: surfaceAngle,
                direction: forward,
                collider: hit.collider);

            return true;
        }
    }
}
