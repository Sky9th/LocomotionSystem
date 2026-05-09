using System;
using UnityEngine;

[Serializable]
public struct SGroundContact
{
    public SGroundContact(bool isGrounded, float distanceToGround, bool isWalkableSlope,
        Vector3 point, Vector3 normal, float stateDuration = 0f)
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
        => new(isGrounded, DistanceToGround, IsWalkableSlope, ContactPoint, ContactNormal, StateDuration);

    public SGroundContact WithStateDuration(float stateDuration)
        => new(IsGrounded, DistanceToGround, IsWalkableSlope, ContactPoint, ContactNormal, stateDuration);

    public static SGroundContact None => new(false, float.PositiveInfinity, false, Vector3.zero, Vector3.up);
}
