using UnityEngine;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Minimal kinematics helper for Locomotion v2.
    ///
    /// Converts high-level move input and configuration values into
    /// planar world-space velocity. This initial version implements
    /// a very simple "desiredSpeed = MoveSpeed * inputMagnitude"
    /// behaviour without acceleration or gravity.
    /// </summary>
    internal static class LocomotionKinematics
    {
        internal static Vector3 ComputePlanarVelocity(
            Vector3 locomotionHeading,
            SPlayerMoveIAction moveAction,
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
    }
}
