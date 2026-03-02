using UnityEngine;
using Game.Locomotion.Config;

internal readonly struct SLocomotionStateContext
{
    /// <summary>World-space character velocity at the start of this step.</summary>
    public readonly Vector3 Velocity;

    /// <summary>Current body forward direction in world space (Y flattened).</summary>
    public readonly Vector3 BodyForward;

    /// <summary>Desired locomotion heading in world space (Y flattened).</summary>
    public readonly Vector3 LocomotionHeading;

    /// <summary>Current ground contact information.</summary>
    public readonly SGroundContact GroundContact;

    /// <summary>Core locomotion capability profile driving simulation thresholds.</summary>
    public readonly LocomotionProfile Profile;

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

    public SLocomotionStateContext(
        Vector3 velocity,
        Vector3 bodyForward,
        Vector3 locomotionHeading,
        SGroundContact groundContact,
        LocomotionProfile profile,
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
        Profile = profile;
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
