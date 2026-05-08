using UnityEngine;
using Game.Locomotion.Discrete.Interface;
using Game.Character.Input;

namespace Game.Locomotion.Discrete.Aspects
{
    /// <summary>
    /// Minimal implementation of the high-level locomotion phase layer.
    ///
    /// Derives a coarse <see cref="ELocomotionPhase"/> value from
    /// world-space velocity and ground contact information.
    /// </summary>
    internal sealed class PhaseAspect : ILocomotionAspect<ELocomotionPhase>
    {
        public ELocomotionPhase Current { get; private set; } = ELocomotionPhase.GroundedIdle;

        public void Reset(ELocomotionPhase defaultState)
        {
            Current = defaultState;
        }

        public void Update(in SCharacterKinematic kinematic, in SLocomotionMotor motor, in SCharacterInputActions actions)
        {
            if (!kinematic.GroundContact.IsGrounded)
            {
                Current = ELocomotionPhase.Airborne;
                return;
            }

            Vector3 velocity = motor.ActualPlanarVelocity;
            velocity.y = 0f;
            float speedSqr = velocity.sqrMagnitude;

            Current = speedSqr <= Mathf.Epsilon
                ? ELocomotionPhase.GroundedIdle
                : ELocomotionPhase.GroundedMoving;
        }
    }
}
