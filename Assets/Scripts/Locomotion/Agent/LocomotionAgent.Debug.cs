using UnityEngine;
using Game.Locomotion.Computation;

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

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            Vector3 forward = snapshot.Motor.LocomotionHeading;

            // Draw locomotion heading in a color based on the high-level state.
            Color headingColor = Color.cyan;
            switch (snapshot.DiscreteState.Phase)
            {
                case ELocomotionPhase.GroundedMoving:
                    headingColor = Color.green;
                    break;
                case ELocomotionPhase.Airborne:
                    headingColor = Color.yellow;
                    break;
            }

            GizmoDebugUtility.DrawArrowLine(transform.position, transform.position + forward * debugForwardLength, headingColor, "Heading");

            /// Ground Debug
            float detectVerticalOffset = Profile.groundDetectVerticalOffset;
            Vector3 rayOrigin = transform.position + Vector3.up * detectVerticalOffset;
            Vector3 standProbeHalfExtents = Profile.groundStandBoxHalfExtents;
            Vector3 standProbeOrigin = rayOrigin;
            float boxCastDistance = standProbeHalfExtents.y + detectVerticalOffset;
            Vector3 boxCastEnd = standProbeOrigin + Vector3.down * boxCastDistance;
            Vector3 boxCastVolumeCenter = (standProbeOrigin + boxCastEnd) * 0.5f;
            Vector3 boxCastVolumeSize = new Vector3(
                standProbeHalfExtents.x * 2f,
                standProbeHalfExtents.y * 2f + boxCastDistance,
                standProbeHalfExtents.z * 2f);

            GizmoDebugUtility.DrawSphere(rayOrigin, 0.01f, Color.red, "Ground Ray Origin");
            GizmoDebugUtility.DrawArrowLine(rayOrigin, rayOrigin + Profile.groundRayLength * Vector3.down, Color.blue, "Ground Detect Ray");
            GizmoDebugUtility.DrawSphere(
                standProbeOrigin,
                0.015f,
                Color.cyan,
                "Ground Probe Start");
            GizmoDebugUtility.DrawWireBox(
                boxCastVolumeCenter,
                boxCastVolumeSize,
                Color.yellow,
                "Ground Probe Sweep");

            if (snapshot.Motor.GroundContact.ContactPoint != Vector3.zero)
            {
                GizmoDebugUtility.DrawSphere(snapshot.Motor.GroundContact.ContactPoint, 0.05f, Color.green, "Ground Contact Point");
            }
            /// Ground Debug end

            /// Obstacle Debug
            Vector3 obstacleForward = snapshot.Motor.LocomotionHeading;
            if (obstacleForward.sqrMagnitude <= Mathf.Epsilon)
            {
                obstacleForward = transform.forward;
            }

            obstacleForward.y = 0f;
            if (obstacleForward.sqrMagnitude > Mathf.Epsilon)
            {
                obstacleForward.Normalize();
            }

            Vector3 obstacleProbeOrigin = transform.position + Vector3.up * Mathf.Max(0f, Profile.obstacleProbeVerticalOffset);
            Vector3 obstacleProbeEnd = obstacleProbeOrigin + obstacleForward * Profile.obstacleProbeDistance;

            GizmoDebugUtility.DrawSphere(obstacleProbeOrigin, 0.02f, new Color(1f, 0.5f, 0f), "Obstacle Probe Origin");
            GizmoDebugUtility.DrawArrowLine(obstacleProbeOrigin, obstacleProbeEnd, new Color(1f, 0.5f, 0f), "Obstacle Detect Ray");

            if (snapshot.Motor.ForwardObstacleDetection.HasHit)
            {
                GizmoDebugUtility.DrawSphere(snapshot.Motor.ForwardObstacleDetection.Point, 0.05f, Color.magenta, "Obstacle Hit Point");
                GizmoDebugUtility.DrawArrowLine(
                    snapshot.Motor.ForwardObstacleDetection.Point,
                    snapshot.Motor.ForwardObstacleDetection.Point + snapshot.Motor.ForwardObstacleDetection.Normal * 0.3f,
                    Color.red,
                    "Obstacle Hit Normal");

                if (snapshot.Motor.ForwardObstacleDetection.HasTopSurface)
                {
                    Vector3 topProbeOrigin = snapshot.Motor.ForwardObstacleDetection.Point + obstacleForward * 0.05f;
                    topProbeOrigin.y = transform.position.y + Profile.obstacleMaxClimbHeight + 0.05f;

                    GizmoDebugUtility.DrawArrowLine(
                        topProbeOrigin,
                        snapshot.Motor.ForwardObstacleDetection.TopPoint,
                        Color.white,
                        "Obstacle Height Probe");
                    GizmoDebugUtility.DrawSphere(snapshot.Motor.ForwardObstacleDetection.TopPoint, 0.05f, Color.green, "Obstacle Top Point");
                }
            }
            /// Obstacle Debug end
        }

    }
#endif

}
