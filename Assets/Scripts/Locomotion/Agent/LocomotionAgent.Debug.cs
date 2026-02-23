using UnityEngine;
using Game.Locomotion.Computation;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Locomotion.Agent
{
    /// <summary>
    /// Debug and gizmo rendering logic for <see cref="LocomotionAgent"/>.
    /// Kept in a separate partial class to keep the core agent logic
    /// focused on simulation and state management.
    /// </summary>
    public partial class LocomotionAgent
    {
        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField, Min(0.1f)] private float debugForwardLength = 2f;

        [Header("Climb Debug")]
        [SerializeField, Min(0f)] private float climbRayDistance = 1.2f;
        [SerializeField, Min(0f)] private float climbForwardClearance = 0.2f;
        [SerializeField, Min(0f)] private float climbGroundProbeDownDistance = 1.5f;
        [SerializeField] private LayerMask climbObstacleLayerMask = ~0;

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            Vector3 origin = modelRoot != null ? modelRoot.position : transform.position;
            Vector3 forward = LocomotionHeading.Evaluate(followAnchor, transform);

            // Draw locomotion heading in a color based on the high-level state.
            Color headingColor = Color.cyan;
            switch (snapshot.State)
            {
                case ELocomotionState.GroundedMoving:
                    headingColor = Color.green;
                    break;
                case ELocomotionState.Airborne:
                    headingColor = Color.yellow;
                    break;
            }

            DrawDebugArrowLine(origin, origin + forward * debugForwardLength, headingColor, "Heading");

            // Visualize ground detection ray and contact point.
            DrawDebugArrowLine(origin, origin + Vector3.down * groundRayLength, Color.magenta, "Ground Ray");

            if (snapshot.GroundContact.IsGrounded)
            {
                Vector3 contactPoint = snapshot.GroundContact.ContactPoint;
                Vector3 contactNormal = snapshot.GroundContact.ContactNormal.normalized;
                Gizmos.DrawSphere(contactPoint, 0.03f);
                DrawDebugArrowLine(contactPoint, contactPoint + contactNormal * 0.3f, Color.white, "Ground Normal");
            }

            // Visualize climb probe rays: step / vault / climb segments
            // and their corresponding ground probes behind obstacles.
            Vector3 flatForward = forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude > Mathf.Epsilon)
            {
                flatForward.Normalize();

                float stepHeight = 0.4f;
                float vaultHeight = 1.0f;
                float climbHeight = 1.6f;

                if (locomotionProfile != null)
                {
                    stepHeight = locomotionProfile.stepClimbHeight;
                    vaultHeight = locomotionProfile.vaultHeight;
                    climbHeight = locomotionProfile.climbHeight;
                }

                DrawClimbDebugSegment(origin, flatForward, stepHeight, Color.blue, "Step");
                DrawClimbDebugSegment(origin, flatForward, vaultHeight, Color.red, "Vault");
                DrawClimbDebugSegment(origin, flatForward, climbHeight, new Color(1f, 0.5f, 0f), "Climb");
            }
        }

        private void DrawClimbDebugSegment(Vector3 origin, Vector3 flatForward, float height, Color color, string label)
        {
            if (height <= 0f || climbRayDistance <= 0f)
            {
                return;
            }

            Vector3 obstacleRayOrigin = origin;
            obstacleRayOrigin.y += height;

            Vector3 rayEnd = obstacleRayOrigin + flatForward * climbRayDistance;
            DrawDebugArrowLine(obstacleRayOrigin, rayEnd, color, label);

            if (Physics.Raycast(
                    obstacleRayOrigin,
                    flatForward,
                    out RaycastHit obstacleHit,
                    climbRayDistance,
                    climbObstacleLayerMask,
                    QueryTriggerInteraction.Ignore))
            {
                Gizmos.DrawSphere(obstacleHit.point, 0.03f);

                // Downward ground probe just beyond the obstacle.
                Vector3 groundProbeOrigin = obstacleHit.point + flatForward * Mathf.Max(climbForwardClearance, 0.01f);
                groundProbeOrigin.y += 0.05f;

                DrawDebugArrowLine(
                    groundProbeOrigin,
                    groundProbeOrigin + Vector3.down * climbGroundProbeDownDistance,
                    Color.yellow,
                    "Ground Probe");

                float maxSlopeAngle = locomotionProfile != null ? locomotionProfile.maxGroundSlopeAngle : 0f;
                SGroundContact climbGround = LocomotionGroundDetection.SampleGround(
                    groundProbeOrigin,
                    climbGroundProbeDownDistance,
                    groundLayerMask,
                    maxSlopeAngle);

                if (climbGround.IsGrounded)
                {
                    Vector3 climbPoint = climbGround.ContactPoint;
                    Vector3 climbNormal = climbGround.ContactNormal.normalized;
                    Gizmos.DrawSphere(climbPoint, 0.03f);
                    DrawDebugArrowLine(climbPoint, climbPoint + climbNormal * 0.3f, Color.green, "Climb Normal");
                }
            }
        }

        /// <summary>
        /// Draws a gizmo line with an arrow head at the end and an optional label.
        /// Used by all locomotion debug drawing so that direction and naming
        /// are consistent across different probes.
        /// </summary>
        private void DrawDebugArrowLine(Vector3 from, Vector3 to, Color color, string label = null)
        {
            if (from == to)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.DrawLine(from, to);

            Vector3 direction = to - from;
            float length = direction.magnitude;
            if (length <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 dirNormalized = direction / length;
            const float arrowSize = 0.15f;
            const float arrowAngle = 20f;

            Quaternion rotLeft = Quaternion.AngleAxis(arrowAngle, Vector3.up);
            Quaternion rotRight = Quaternion.AngleAxis(-arrowAngle, Vector3.up);

            Vector3 leftDir = rotLeft * -dirNormalized;
            Vector3 rightDir = rotRight * -dirNormalized;

            Gizmos.DrawLine(to, to + leftDir * arrowSize);
            Gizmos.DrawLine(to, to + rightDir * arrowSize);

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(label))
            {
                Vector3 midPoint = (from + to) * 0.5f;
                Handles.color = Color.white;
                Handles.Label(midPoint, label);
            }
#endif
        }
    }
}
