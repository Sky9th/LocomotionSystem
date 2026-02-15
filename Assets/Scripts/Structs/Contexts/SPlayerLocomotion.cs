using System;
using UnityEngine;

/// <summary>
/// Immutable snapshot describing the player's locomotion state at a given frame.
/// </summary>
[Serializable]
public struct SPlayerLocomotion
{
    public SPlayerLocomotion(
        Vector3 position,
        Vector3 velocity,
        Vector3 locomotionHeading,
        Vector3 bodyForward,
        Vector2 localVelocity,
        Vector2 lookDirection,
        SLocomotionDiscreteState discreteState,
        SGroundContact groundContact,
        float turnAngle,
        bool isTurning,
        bool isLeftFootOnFront,
        EPostureState posture,
        EMovementGait gait,
        ELocomotionCondition condition)
    {
        Position = position;
        Velocity = velocity;
        LocomotionHeading = locomotionHeading.sqrMagnitude > Mathf.Epsilon ? locomotionHeading.normalized : Vector3.forward;
        BodyForward = bodyForward.sqrMagnitude > Mathf.Epsilon ? bodyForward.normalized : Vector3.forward;
        LocalVelocity = localVelocity;
        LookDirection = lookDirection;
        DiscreteState = discreteState;
        GroundContact = groundContact;
        TurnAngle = turnAngle;
        IsTurning = isTurning;
        IsLeftFootOnFront = isLeftFootOnFront;
        Posture = posture;
        Gait = gait;
        Condition = condition;
    }

    public Vector3 Position { get; }
    public Vector3 Velocity { get; }
    public Vector3 LocomotionHeading { get; }
    public Vector3 BodyForward { get; }
    public Vector2 LocalVelocity { get; }
    public Vector2 LookDirection { get; }
    public SLocomotionDiscreteState DiscreteState { get; }
    public ELocomotionState State => DiscreteState.State;
    public SGroundContact GroundContact { get; }
    public float TurnAngle { get; }
    public bool IsTurning { get; }
    public bool IsLeftFootOnFront { get; }

    /// <summary>Current posture (e.g. Standing / Crouching / Prone).</summary>
    public EPostureState Posture { get; }

    /// <summary>Current movement gait (Idle / Walk / Run / Sprint / Crawl).</summary>
    public EMovementGait Gait { get; }

    /// <summary>Additional locomotion condition modifiers (e.g. injured, heavy load).</summary>
    public ELocomotionCondition Condition { get; }

    public float Speed => Velocity.magnitude;
    public bool HasMovement => Velocity.sqrMagnitude > Mathf.Epsilon;
    public bool IsGrounded => GroundContact.IsGrounded;

    public static SPlayerLocomotion Default => new SPlayerLocomotion(
        Vector3.zero,
        Vector3.zero,
        Vector3.forward,
        Vector3.forward,
        Vector2.zero,
        Vector2.zero,
        new SLocomotionDiscreteState(
            ELocomotionState.GroundedIdle,
            EPostureState.Standing,
            EMovementGait.Idle,
            ELocomotionCondition.Normal),
        SGroundContact.None,
        0f,
        false,
        true,
        EPostureState.Standing,
        EMovementGait.Idle,
        ELocomotionCondition.Normal);
}
