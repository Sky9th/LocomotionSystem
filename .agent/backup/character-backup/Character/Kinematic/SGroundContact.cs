using System;
using UnityEngine;

/// <summary>
/// Describes the current ground contact state for the character.
/// </summary>
[Serializable]
public struct SGroundContact
{
    public SGroundContact(
        bool isGrounded,
        float distanceToGround,
        bool isWalkableSlope,
        Vector3 point,
        Vector3 normal,
        float stateDuration = 0f)
    {
        IsGrounded = isGrounded;
        DistanceToGround = distanceToGround;
        IsWalkableSlope = isWalkableSlope;
        ContactPoint = point;
        ContactNormal = normal;
        StateDuration = stateDuration;
    }

    public bool IsGrounded { get; }
    public float DistanceToGround { get; }
    public bool IsWalkableSlope { get; }
    public Vector3 ContactPoint { get; }
    public Vector3 ContactNormal { get; }
    public float StateDuration { get; }

    public SGroundContact WithIsGrounded(bool isGrounded)
        => new SGroundContact(
            isGrounded,
            DistanceToGround,
            IsWalkableSlope,
            ContactPoint,
            ContactNormal,
            StateDuration);

    public SGroundContact WithStateDuration(float stateDuration)
        => new SGroundContact(
            IsGrounded,
            DistanceToGround,
            IsWalkableSlope,
            ContactPoint,
            ContactNormal,
            stateDuration);

    public static SGroundContact None => new SGroundContact(
        isGrounded: false,
        distanceToGround: float.PositiveInfinity,
        isWalkableSlope: false,
        point: Vector3.zero,
        normal: Vector3.up,
        stateDuration: 0f);
}
