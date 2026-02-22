using UnityEngine;

using Game.Locomotion.Animation.Config;

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
            LocomotionAnimationProfile config)
        {
            if (moveAction.Equals(SMoveIAction.None) || !moveAction.HasInput || config == null)
            {
                return Vector2.zero;
            }

            // Derive local planar velocity from raw move input so that
            // pressing A, for example, results in a purely leftward
            // local velocity.
            Vector2 input = moveAction.RawInput;
            float intensity = Mathf.Clamp01(input.magnitude);
            float speed = intensity * config.moveSpeed;

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
