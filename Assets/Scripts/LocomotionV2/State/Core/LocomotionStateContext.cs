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
        public readonly SMoveIAction MoveAction;

        /// <summary>Aggregated look intent for this frame.</summary>
        public readonly SLookIAction LookAction;

        /// <summary>Discrete crouch intent for this frame.</summary>
        public readonly SCrouchIAction CrouchAction;

        /// <summary>Discrete prone intent for this frame.</summary>
        public readonly SProneIAction ProneAction;

        /// <summary>Discrete walk intent for this frame.</summary>
        public readonly SWalkIAction WalkAction;

        /// <summary>Discrete run intent for this frame.</summary>
        public readonly SRunIAction RunAction;

        /// <summary>Discrete sprint intent for this frame.</summary>
        public readonly SSprintIAction SprintAction;

        /// <summary>Discrete jump intent for this frame.</summary>
        public readonly SJumpIAction JumpAction;

        /// <summary>Discrete stand intent for this frame.</summary>
        public readonly SStandIAction StandAction;

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
            SMoveIAction moveAction,
            SLookIAction lookAction,
            SCrouchIAction crouchAction,
            SProneIAction proneAction,
            SWalkIAction walkAction,
            SRunIAction runAction,
            SSprintIAction sprintAction,
            SJumpIAction jumpAction,
            SStandIAction standAction)
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
            WalkAction = walkAction;
            RunAction = runAction;
            SprintAction = sprintAction;
            JumpAction = jumpAction;
            StandAction = standAction;
        }
    }
}
