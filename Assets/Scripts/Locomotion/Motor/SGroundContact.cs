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
        Vector3 normal)
    {
        IsGrounded = isGrounded;
        DistanceToGround = distanceToGround;
        IsWalkableSlope = isWalkableSlope;
        ContactPoint = point;
        ContactNormal = normal;
    }

    public bool IsGrounded { get; }
    public float DistanceToGround { get; }
    public bool IsWalkableSlope { get; }
    public Vector3 ContactPoint { get; }
    public Vector3 ContactNormal { get; }

    public static SGroundContact None => new SGroundContact(
        isGrounded: false,
        distanceToGround: float.PositiveInfinity,
        isWalkableSlope: false,
        point: Vector3.zero,
        normal: Vector3.up);
}
