using UnityEngine;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Minimal kinematics helper for Locomotion v2.
    ///
    /// Converts high-level move input and configuration values into
    /// planar velocities. Exposes helpers for computing a desired
    /// local planar velocity and converting it to world-space, as
    /// well as smoothing towards that velocity over time.
    /// </summary>
    internal static class LocomotionKinematics
    {
        /// <summary>
        /// Computes the desired planar velocity in character-local space
        /// based on move input and configuration. X is strafe (left/right)
        /// and Y is forward/back.
        /// </summary>
        internal static Vector2 ComputeDesiredPlanarVelocity(
            SMoveIAction moveAction,
            float moveSpeed)
        {
            if (moveAction.Equals(SMoveIAction.None) || !moveAction.HasInput || moveSpeed <= 0f)
            {
                return Vector2.zero;
            }

            // Derive local planar velocity from raw move input so that
            // pressing A, for example, results in a purely leftward
            // local velocity.
            Vector2 input = moveAction.RawInput;
            float intensity = Mathf.Clamp01(input.magnitude);
            float speed = intensity * moveSpeed;

            if (input.sqrMagnitude > Mathf.Epsilon)
            {
                input = input.normalized;
            }

            return input * speed;
        }

        /// <summary>
        /// Converts a local-space planar velocity into world-space using
        /// the given locomotion heading as the forward axis.
        /// </summary>
        internal static Vector3 ConvertLocalToWorldPlanarVelocity(
            Vector2 localVelocity,
            Vector3 locomotionHeading)
        {
            Vector3 forward = locomotionHeading;
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return forward * localVelocity.y + right * localVelocity.x;
        }

        /// <summary>
        /// Computes the signed planar angle (XZ) between current body forward
        /// and desired locomotion heading. Positive values mean turning right.
        /// </summary>
        internal static float ComputeSignedPlanarTurnAngle(
            Vector3 bodyForward,
            Vector3 locomotionHeading)
        {
            Vector3 bodyFlat = bodyForward;
            Vector3 headingFlat = locomotionHeading;
            bodyFlat.y = 0f;
            headingFlat.y = 0f;

            if (bodyFlat.sqrMagnitude <= Mathf.Epsilon || headingFlat.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            bodyFlat.Normalize();
            headingFlat.Normalize();

            float signedAngle = Vector3.SignedAngle(bodyFlat, headingFlat, Vector3.up);
            return Mathf.Clamp(signedAngle, -180f, 180f);
        }

        internal static Vector3 SmoothVelocity(
            Vector3 currentVelocity,
            Vector3 desiredVelocity,
            float acceleration,
            float deltaTime)
        {
            if (acceleration <= 0f || deltaTime <= 0f)
            {
                return desiredVelocity;
            }

            // Move current velocity towards desired velocity with a
            // simple acceleration-limited step.
            float maxDelta = acceleration * deltaTime;
            return Vector3.MoveTowards(currentVelocity, desiredVelocity, maxDelta);
        }
    }
}
