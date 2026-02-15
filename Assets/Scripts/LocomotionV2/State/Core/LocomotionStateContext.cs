using UnityEngine;

namespace Game.Locomotion.State.Core
{
    /// <summary>
    /// Read-only input snapshot for the new locomotion state module.
    ///
    /// This context is the single entry payload passed into state
    /// controllers and state machines so they are decoupled from
    /// MonoBehaviour and other game-specific details.
    /// </summary>
    internal readonly struct LocomotionStateContext
    {
        /// <summary>World-space character velocity at the start of this step.</summary>
        public readonly Vector3 Velocity;

        /// <summary>Current body forward direction in world space (Y flattened).</summary>
        public readonly Vector3 BodyForward;

        /// <summary>Desired locomotion heading in world space (Y flattened).</summary>
        public readonly Vector3 LocomotionHeading;

        /// <summary>Aggregated planar movement intent for this frame.</summary>
        public readonly SPlayerMoveIAction MoveAction;

        /// <summary>Aggregated look intent for this frame.</summary>
        public readonly SPlayerLookIAction LookAction;

        /// <summary>Discrete crouch intent for this frame.</summary>
        public readonly SPlayerCrouchIAction CrouchAction;

        /// <summary>Discrete prone intent for this frame.</summary>
        public readonly SPlayerProneIAction ProneAction;

        /// <summary>Discrete jump intent for this frame.</summary>
        public readonly SPlayerJumpIAction JumpAction;

        /// <summary>Discrete stand intent for this frame.</summary>
        public readonly SPlayerStandIAction StandAction;

        /// <summary>Current ground contact information.</summary>
        public readonly SGroundContact GroundContact;

        /// <summary>Configuration profile driving locomotion thresholds.</summary>
        public readonly LocomotionConfigProfile Config;

        /// <summary>Last frame's discrete locomotion state, if available.</summary>
        public readonly SLocomotionDiscreteState PreviousState;

        public LocomotionStateContext(
            Vector3 velocity,
            Vector3 bodyForward,
            Vector3 locomotionHeading,
            SGroundContact groundContact,
            LocomotionConfigProfile config,
            SLocomotionDiscreteState previousState,
            SPlayerMoveIAction moveAction,
            SPlayerLookIAction lookAction,
            SPlayerCrouchIAction crouchAction,
            SPlayerProneIAction proneAction,
            SPlayerJumpIAction jumpAction,
            SPlayerStandIAction standAction)
        {
            Velocity = velocity;
            BodyForward = bodyForward;
            LocomotionHeading = locomotionHeading;
            GroundContact = groundContact;
            Config = config;
            PreviousState = previousState;

            MoveAction = moveAction;
            LookAction = lookAction;
            CrouchAction = crouchAction;
            ProneAction = proneAction;
            JumpAction = jumpAction;
            StandAction = standAction;
        }
    }
}
