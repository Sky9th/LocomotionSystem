using UnityEngine;
using Game.Locomotion.Computation;

namespace Game.Locomotion.LegacyControl
{
    /// <summary>
    /// Encapsulates locomotion movement state for a character.
    ///
    /// Holds the smoothed world-space velocity and the last non-zero
    /// move input direction, and provides a single entry point to
    /// update world and local planar velocity each frame.
    /// </summary>
    internal sealed class LocomotionMovementState
    {
        private Vector3 currentVelocity = Vector3.zero;
        private Vector2 lastMoveInput = Vector2.zero;

        public Vector3 CurrentVelocity => currentVelocity;

        public void Reset()
        {
            currentVelocity = Vector3.zero;
            lastMoveInput = Vector2.zero;
        }

        public void Update(
            Vector3 locomotionHeading,
            SMoveIAction moveAction,
            LocomotionConfigProfile config,
            float deltaTime,
            out Vector3 worldVelocity,
            out Vector2 localVelocity)
        {
            // Compute desired planar velocity and smooth towards it.
            Vector3 desiredVelocity = LocomotionKinematics.ComputeDesiredPlanarVelocity(locomotionHeading, moveAction, config);
            float acceleration = config != null ? config.Acceleration : 0f;
            currentVelocity = LocomotionKinematics.SmoothVelocity(currentVelocity, desiredVelocity, acceleration, deltaTime);

            worldVelocity = currentVelocity;

            // Derive local planar velocity using shared planar velocity helper.
            LocomotionPlanarVelocity.Evaluate(
                currentVelocity,
                locomotionHeading,
                moveAction,
                ref lastMoveInput,
                out localVelocity);
        }
    }
}
