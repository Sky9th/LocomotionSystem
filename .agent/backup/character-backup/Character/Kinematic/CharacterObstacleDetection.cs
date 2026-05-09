using UnityEngine;

namespace Game.Character.Probes
{
    /// <summary>
    /// Forward obstacle probe utilities for locomotion traversal.
    /// This first iteration only answers whether a blocking surface
    /// exists ahead and returns the raw hit information.
    /// </summary>
    internal static class CharacterObstacleDetection
    {
        private const float HeightProbeVerticalPadding = 0.05f;
        private const float HeightProbeForwardInset = 0.05f;

        internal static bool TryDetectForwardObstacle(
            Vector3 actorPosition,
            Vector3 forward,
            float probeVerticalOffset,
            float probeDistance,
            int layerMask,
            float maxClimbHeight,
            float maxSlopeAngleDegrees,
            out SForwardObstacleDetection obstacleDetection)
        {
            obstacleDetection = SForwardObstacleDetection.None;

            if (probeDistance <= 0f)
            {
                return false;
            }

            Vector3 direction = forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            direction.Normalize();

            Vector3 origin = actorPosition + Vector3.up * Mathf.Max(0f, probeVerticalOffset);

            if (!Physics.Raycast(
                    origin,
                    direction,
                    out RaycastHit hitInfo,
                    probeDistance,
                    layerMask,
                    QueryTriggerInteraction.Ignore))
            {
                obstacleDetection = new SForwardObstacleDetection(
                    hasHit: false,
                    hasTopSurface: false,
                    isSlope: false,
                    isObstacle: false,
                    canClimb: false,
                    canVault: false,
                    canStepOver: false,
                    distance: float.PositiveInfinity,
                    obstacleHeight: float.PositiveInfinity,
                    point: Vector3.zero,
                    normal: Vector3.zero,
                    topPoint: Vector3.zero,
                    topNormal: Vector3.up,
                    surfaceAngle: 90f,
                    direction: direction,
                    collider: null);
                return false;
            }

            float surfaceAngle = Vector3.Angle(hitInfo.normal, Vector3.up);
            bool isSlope = CharacterGroundDetection.IsWalkableSlope(hitInfo.normal, maxSlopeAngleDegrees);
            bool isObstacle = !isSlope;
            bool hasTopSurface = false;
            bool canClimb = false;
            float obstacleHeight = float.PositiveInfinity;
            Vector3 topPoint = Vector3.zero;
            Vector3 topNormal = Vector3.up;

            if (isObstacle)
            {
                Vector3 heightProbeOrigin = hitInfo.point + direction * HeightProbeForwardInset;
                heightProbeOrigin.y = actorPosition.y + maxClimbHeight + HeightProbeVerticalPadding;
                float heightProbeDistance = maxClimbHeight + HeightProbeVerticalPadding * 2f;

                if (Physics.Raycast(
                        heightProbeOrigin,
                        Vector3.down,
                        out RaycastHit topHitInfo,
                        heightProbeDistance,
                        layerMask,
                        QueryTriggerInteraction.Ignore))
                {
                    hasTopSurface = true;
                    topPoint = topHitInfo.point;
                    topNormal = topHitInfo.normal;
                    obstacleHeight = Mathf.Max(0f, topHitInfo.point.y - actorPosition.y);
                    canClimb = obstacleHeight <= maxClimbHeight;
                }
            }

            obstacleDetection = new SForwardObstacleDetection(
                hasHit: true,
                hasTopSurface: hasTopSurface,
                isSlope: isSlope,
                isObstacle: isObstacle,
                canClimb: canClimb,
                canVault: false,
                canStepOver: false,
                distance: Mathf.Max(0f, hitInfo.distance),
                obstacleHeight: obstacleHeight,
                point: hitInfo.point,
                normal: hitInfo.normal,
                topPoint: topPoint,
                topNormal: topNormal,
                surfaceAngle: surfaceAngle,
                direction: direction,
                collider: hitInfo.collider);
            return true;
        }
    }
}