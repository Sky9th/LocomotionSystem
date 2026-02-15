using UnityEngine;

namespace Game.Locomotion.LegacyControl
{
    /// <summary>
    /// Minimal high-level locomotion state resolver for Locomotion v2.
    ///
    /// Uses current velocity and ground contact information to
    /// derive a coarse ELocomotionState value.
    /// </summary>
    internal static class LocomotionStateResolver
    {
        internal static ELocomotionState ResolveHighLevelState(
            Vector3 velocity,
            SGroundContact groundContact)
        {
            if (!groundContact.IsGrounded)
            {
                return ELocomotionState.Airborne;
            }

            float speedSqr = velocity.sqrMagnitude;
            if (speedSqr <= Mathf.Epsilon)
            {
                return ELocomotionState.GroundedIdle;
            }

            return ELocomotionState.GroundedMoving;
        }
    }
}
