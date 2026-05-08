using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Character.Components
{
    public partial class CharacterActor
    {
        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField, Min(0.1f)] private float debugArrowLength = 2f;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos) return;

            GameContext context = GameContext.Instance;
            if (context == null || !context.TryGetSnapshot(out SCharacterSnapshot snapshot)) return;

            var pos = transform.position;
            var mot = snapshot.Locomotion.Motor;
            var disc = snapshot.Locomotion.Discrete;
            var kin = snapshot.Kinematic;

            DrawTextLabel(pos, disc, mot);
            DrawHeading(pos, kin.LocomotionHeading, disc.Phase);
            DrawBodyForward(pos, kin.BodyForward);
            DrawVelocity(pos, mot.ActualPlanarVelocity);
            DrawGround(pos, kin);
            DrawObstacle(pos, kin);
        }

        private static void DrawTextLabel(Vector3 pos, SCharacterDiscrete disc, SCharacterMotor mot)
        {
            Handles.Label(
                pos + Vector3.up * 2.2f,
                $"{disc.Phase} | {disc.Gait} | {disc.Posture} | Turn:{disc.IsTurning} | {mot.ActualPlanarVelocity.magnitude:F1}m/s");
        }

        private void DrawHeading(Vector3 pos, Vector3 heading, ELocomotionPhase phase)
        {
            if (heading.sqrMagnitude <= Mathf.Epsilon) return;
            var color = phase switch
            {
                ELocomotionPhase.GroundedMoving => Color.green,
                ELocomotionPhase.Airborne       => Color.yellow,
                _                               => Color.cyan
            };
            GizmoDebugUtility.DrawArrowLine(pos, pos + heading * debugArrowLength, color, "Heading");
        }

        private void DrawBodyForward(Vector3 pos, Vector3 bodyForward)
        {
            if (bodyForward.sqrMagnitude <= Mathf.Epsilon) return;
            GizmoDebugUtility.DrawArrowLine(pos, pos + bodyForward * (debugArrowLength * 0.7f), Color.blue, "BodyFwd");
        }

        private static void DrawVelocity(Vector3 pos, Vector3 velocity)
        {
            if (velocity.sqrMagnitude <= 0.01f) return;
            GizmoDebugUtility.DrawArrowLine(pos, pos + velocity, Color.white, "Vel");
        }

        private void DrawGround(Vector3 pos, SCharacterKinematic kin)
        {
            float probeHeight = characterProfile.groundProbeHeight;
            float probeRadius = characterProfile.groundProbeRadius;

            var origin = pos + Vector3.up * probeHeight;
            var maxDist = probeHeight + 10f;
            GizmoDebugUtility.DrawArrowLine(origin, origin + Vector3.down * maxDist, Color.blue, "Ground Probe");

            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(origin, probeRadius);

            if (kin.GroundContact.IsGrounded)
            {
                GizmoDebugUtility.DrawSphere(kin.GroundContact.ContactPoint, 0.05f, Color.green, "Contact");
            }
        }

        private void DrawObstacle(Vector3 pos, SCharacterKinematic kin)
        {
            var obs = kin.ForwardObstacleDetection;
            if (!obs.HasHit) return;

            GizmoDebugUtility.DrawSphere(obs.Point, 0.05f, Color.magenta, "Hit");
            GizmoDebugUtility.DrawArrowLine(obs.Point, obs.Point + obs.Normal * 0.3f, Color.red, "Normal");

            if (!obs.HasTopSurface) return;

            float groundY = kin.GroundContact.IsGrounded ? kin.GroundContact.ContactPoint.y : pos.y;
            var groundPoint = new Vector3(obs.Point.x, groundY, obs.Point.z);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundPoint, groundPoint + Vector3.up * characterProfile.obstacleMaxClimbHeight);

            var topOrigin = new Vector3(obs.Point.x, groundY + characterProfile.obstacleMaxClimbHeight, obs.Point.z);
            GizmoDebugUtility.DrawArrowLine(topOrigin, obs.TopPoint, Color.white, "H Probe");

            GizmoDebugUtility.DrawSphere(obs.TopPoint, 0.05f, Color.green, "Top");

            Handles.Label(obs.Point + Vector3.up * 0.3f + Vector3.right * 0.2f, $"H:{obs.ObstacleHeight:F2}m");
        }
#endif
    }
}
