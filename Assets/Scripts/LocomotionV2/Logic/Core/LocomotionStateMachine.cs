using UnityEngine;

namespace Game.Locomotion.LegacyControl
{
    /// <summary>
    /// Central controller for discrete locomotion state.
    ///
    /// Aggregates high-level locomotion state, posture, gait and
    /// condition into a single evaluation step so that the logic
    /// for managing different stances lives in one place.
    /// </summary>
    internal sealed class LocomotionStateMachine
    {
        /// <summary>Current high-level locomotion state (Grounded/Airborne...).</summary>
        public ELocomotionState LocomotionState { get; private set; } = ELocomotionState.GroundedIdle;

        /// <summary>Current posture (Standing/Crouching/Prone...).</summary>
        public EPostureState Posture { get; private set; } = EPostureState.Standing;

        /// <summary>Current movement gait (Idle/Walk/Run/Sprint...).</summary>
        public EMovementGait Gait { get; private set; } = EMovementGait.Idle;

        /// <summary>Current locomotion condition (Normal/Injured...).</summary>
        public ELocomotionCondition Condition { get; private set; } = ELocomotionCondition.Normal;

        /// <summary>Resets all discrete state to safe defaults.</summary>
        public void Reset()
        {
            LocomotionState = ELocomotionState.GroundedIdle;
            Posture = EPostureState.Standing;
            Gait = EMovementGait.Idle;
            Condition = ELocomotionCondition.Normal;
        }

        /// <summary>
        /// Evaluate the current discrete locomotion state from the
        /// supplied movement and ground contact information.
        ///
        /// For now this mirrors the legacy behaviour (stateless
        /// mapping from inputs), but centralising the logic here
        /// makes it easier to extend with richer posture/condition
        /// rules later.
        /// </summary>
        public SLocomotionDiscreteState Evaluate(
            Vector3 velocity,
            SGroundContact groundContact,
            LocomotionConfigProfile config)
        {
            // Derive gait from world-space velocity and config.
            Gait = LocomotionGaitResolver.ResolveMovementGait(velocity, config);

            // Derive high-level locomotion state from velocity and ground contact.
            LocomotionState = LocomotionStateResolver.ResolveHighLevelState(velocity, groundContact);

            // Posture and condition are still simple placeholders.
            // They are kept as explicit fields here so they can be
            // evolved into proper state machines without touching
            // external callers.
            Posture = EPostureState.Standing;
            Condition = ELocomotionCondition.Normal;

            return new SLocomotionDiscreteState(LocomotionState, Posture, Gait, Condition);
        }
    }
}
