using Game.Locomotion.Animation.Config;
using UnityEngine;

internal readonly struct SLocomotionStateContext
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
    public readonly LocomotionAnimationProfile Config;

    /// <summary>Last frame's discrete locomotion state, if available.</summary>
    public readonly SLocomotionDiscreteState PreviousState;

    public SLocomotionStateContext(
        Vector3 velocity,
        Vector3 bodyForward,
        Vector3 locomotionHeading,
        SGroundContact groundContact,
        LocomotionAnimationProfile config,
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
