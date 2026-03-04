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
            switch (snapshot.DiscreteState.Phase)
            {
                case ELocomotionPhase.GroundedMoving:
                    headingColor = Color.green;
                    break;
                case ELocomotionPhase.Airborne:
                    headingColor = Color.yellow;
                    break;
            }

            DrawDebugArrowLine(origin, origin + forward * debugForwardLength, headingColor, "Heading");

            // Visualize ground detection ray and contact point.
            DrawDebugArrowLine(origin, origin + Vector3.down * groundRayLength, Color.magenta, "Ground Ray");

            if (snapshot.Agent.GroundContact.IsGrounded)
            {
                Vector3 contactPoint = snapshot.Agent.GroundContact.ContactPoint;
                Vector3 contactNormal = snapshot.Agent.GroundContact.ContactNormal.normalized;
                DrawDebugArrowLine(contactPoint, contactPoint + contactNormal * 0.3f, Color.white, "Ground Normal");
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

        // Note: Traversal/climb probing debug was removed.
    }
}
