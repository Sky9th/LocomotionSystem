using UnityEngine;
using Game.Locomotion.State.Core;

namespace Game.Locomotion.State.Layers
{
    /// <summary>
    /// Minimal implementation of the high-level locomotion phase layer.
    ///
    /// Derives a coarse <see cref="ELocomotionState"/> value from
    /// world-space velocity and ground contact information.
    /// </summary>
    internal sealed class PhaseStateLayer : ILocomotionStateLayer<ELocomotionState>
    {
        public ELocomotionState Current { get; private set; } = ELocomotionState.GroundedIdle;

        public void Reset(ELocomotionState defaultState)
        {
            Current = defaultState;
        }

        public void Update(in LocomotionStateContext context)
        {
            if (!context.GroundContact.IsGrounded)
            {
                Current = ELocomotionState.Airborne;
                return;
            }

            Vector3 velocity = context.Velocity;
            velocity.y = 0f;
            float speedSqr = velocity.sqrMagnitude;

            Current = speedSqr <= Mathf.Epsilon
                ? ELocomotionState.GroundedIdle
                : ELocomotionState.GroundedMoving;
        }
    }
}
