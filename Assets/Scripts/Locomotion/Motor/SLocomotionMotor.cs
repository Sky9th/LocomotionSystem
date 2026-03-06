using System;
using UnityEngine;

/// <summary>
/// Aggregated output produced by <see cref="Game.Locomotion.Agent.LocomotionAgent"/>.
/// This struct intentionally contains only data the agent can publish each frame
/// (pose, kinematics and derived probes such as ground contact).
/// </summary>
[Serializable]
public struct SLocomotionMotor
{
    public SLocomotionMotor(
        Vector3 position,
        Vector2 desiredLocalVelocity,
        Vector3 desiredPlanarVelocity,
        Vector2 actualLocalVelocity,
        Vector3 actualPlanarVelocity,
        float actualSpeed,
        Vector3 locomotionHeading,
        Vector3 bodyForward,
        Vector2 lookDirection,
        SGroundContact groundContact,
        float turnAngle,
        bool isLeftFootOnFront)
    {
        Position = position;
        DesiredLocalVelocity = desiredLocalVelocity;
        DesiredPlanarVelocity = desiredPlanarVelocity;
        ActualLocalVelocity = actualLocalVelocity;
        ActualPlanarVelocity = actualPlanarVelocity;
        ActualSpeed = actualSpeed;
        LocomotionHeading = locomotionHeading.sqrMagnitude > Mathf.Epsilon ? locomotionHeading.normalized : Vector3.forward;
        BodyForward = bodyForward.sqrMagnitude > Mathf.Epsilon ? bodyForward.normalized : Vector3.forward;
        LookDirection = lookDirection;
        GroundContact = groundContact;
        TurnAngle = turnAngle;
        IsLeftFootOnFront = isLeftFootOnFront;
    }

    public Vector3 Position { get; }
    public Vector2 DesiredLocalVelocity { get; }
    public Vector3 DesiredPlanarVelocity { get; }
    public Vector2 ActualLocalVelocity { get; }
    public Vector3 ActualPlanarVelocity { get; }
    public float ActualSpeed { get; }
    public Vector3 LocomotionHeading { get; }
    public Vector3 BodyForward { get; }
    public Vector2 LookDirection { get; }
    public SGroundContact GroundContact { get; }
    public float TurnAngle { get; }
    public bool IsLeftFootOnFront { get; }

    public static SLocomotionMotor Default => new SLocomotionMotor(
        Vector3.zero,
        Vector2.zero,
        Vector3.zero,
        Vector2.zero,
        Vector3.zero,
        0f,
        Vector3.forward,
        Vector3.forward,
        Vector2.zero,
        SGroundContact.None,
        0f,
        true);
}
