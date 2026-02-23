using UnityEngine;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Helper used to probe for climb / vault opportunities by
    /// checking for an obstacle in front of the character and
    /// sampling for walkable ground just behind that obstacle.
    /// </summary>
    internal static class LocomotionClimbProbe
    {
        /// <summary>
        /// Casts a ray forward to detect an obstacle, then casts a
        /// ray down just beyond that obstacle to see if there is
        /// walkable ground (within a maximum slope) behind it.
        /// </summary>
        /// <param name="origin">Character position (typically at feet or hips).</param>
        /// <param name="forward">Desired facing / movement direction on XZ plane.</param>
        /// <param name="obstacleCheckDistance">Maximum distance to search for an obstacle.</param>
        /// <param name="obstacleCheckHeight">
        /// Height above origin at which the forward ray is cast. This
        /// roughly corresponds to the part of the body that would
        /// contact the obstacle (e.g. chest height).
        /// </param>
        /// <param name="forwardClearance">
        /// Small offset to move past the detected obstacle before
        /// casting the downward ground probe.
        /// </param>
        /// <param name="groundProbeDownDistance">Maximum distance to search for ground below the probe point.</param>
        /// <param name="obstacleLayerMask">Layer mask used when detecting the obstacle.</param>
        /// <param name="groundLayerMask">Layer mask used when sampling ground behind the obstacle.</param>
        /// <param name="maxSlopeAngleDegrees">Maximum allowed ground slope in degrees.</param>
        /// <param name="groundContact">Ground contact information behind the obstacle, if any.</param>
        /// <returns>
        /// True if an obstacle was detected within the given distance
        /// and valid walkable ground was found just behind it.
        /// </returns>
        internal static bool TryFindGroundBehindObstacle(
            Vector3 origin,
            Vector3 forward,
            float obstacleCheckDistance,
            float obstacleCheckHeight,
            float forwardClearance,
            float groundProbeDownDistance,
            int obstacleLayerMask,
            int groundLayerMask,
            float maxSlopeAngleDegrees,
            out SGroundContact groundContact)
        {
            groundContact = SGroundContact.None;

            if (obstacleCheckDistance <= 0f || groundProbeDownDistance <= 0f)
            {
                return false;
            }

            // Flatten forward to XZ and normalise.
            Vector3 flatForward = forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }
            flatForward.Normalize();

            // 1) Forward ray at the desired check height to find an obstacle.
            Vector3 obstacleRayOrigin = origin;
            obstacleRayOrigin.y += obstacleCheckHeight;

            Ray obstacleRay = new Ray(obstacleRayOrigin, flatForward);
            if (!Physics.Raycast(obstacleRay, out RaycastHit hitInfo, obstacleCheckDistance, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // 2) Start the ground probe slightly beyond the obstacle and a
            // bit above the hit point to reduce the chance of starting
            // exactly inside geometry.
            Vector3 groundProbeOrigin = hitInfo.point + flatForward * Mathf.Max(forwardClearance, 0.01f);
            groundProbeOrigin.y += 0.05f;

            groundContact = LocomotionGroundDetection.SampleGround(
                groundProbeOrigin,
                groundProbeDownDistance,
                groundLayerMask,
                maxSlopeAngleDegrees);

            return groundContact.IsGrounded;
        }
    }
}
