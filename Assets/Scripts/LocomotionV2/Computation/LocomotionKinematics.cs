using UnityEngine;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Minimal kinematics helper for Locomotion v2.
    ///
    /// Converts high-level move input and configuration values into
    /// planar world-space velocity. Exposes helpers for computing a
    /// desired velocity and then smoothing towards it over time.
    /// </summary>
    internal static class LocomotionKinematics
    {
        internal static Vector3 ComputeDesiredPlanarVelocity(
            Vector3 locomotionHeading,
            SMoveIAction moveAction,
            LocomotionConfigProfile config)
        {
            if (config == null || !moveAction.HasInput)
            {
                return Vector3.zero;
            }

            // Use the magnitude of the input vector as an intensity factor
            // and scale it by the configured move speed.
            float intensity = Mathf.Clamp01(moveAction.RawInput.magnitude);
            float speed = intensity * config.MoveSpeed;

            Vector3 heading = locomotionHeading;
            heading.y = 0f;
            if (heading.sqrMagnitude <= Mathf.Epsilon)
            {
                heading = Vector3.forward;
            }
            return heading.normalized * speed;
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
