using System;
using UnityEngine;

/// <summary>
/// 角色当前的移动快照。首版聚焦 Idle/Walk，所以只存储位置、速度、朝向与地面信息。
/// </summary>
[Serializable]
public struct SPlayerLocomotion
{
    public SPlayerLocomotion(
        Vector3 position,
        Vector3 velocity,
        Vector3 forward,
        Vector2 localVelocity,
        Vector2 lookDirection,
        ELocomotionState state,
        SGroundContact groundContact,
        float turnAngle,
        bool isTurning,
        bool isLeftFootOnFront)
    {
        Position = position;
        Velocity = velocity;
        Forward = forward.sqrMagnitude > Mathf.Epsilon ? forward.normalized : Vector3.forward;
        LocalVelocity = localVelocity;
        LookDirection = lookDirection;
        State = state;
        GroundContact = groundContact;
        TurnAngle = turnAngle;
        IsTurning = isTurning;
        IsLeftFootOnFront = isLeftFootOnFront;
    }

    public Vector3 Position { get; }
    public Vector3 Velocity { get; }
    public Vector3 Forward { get; }
    public Vector2 LocalVelocity { get; }
    public Vector2 LookDirection { get; }
    public ELocomotionState State { get; }
    public SGroundContact GroundContact { get; }
    public float TurnAngle { get; }
    public bool IsTurning { get; }
    public bool IsLeftFootOnFront { get; }

    public float Speed => Velocity.magnitude;
    public bool HasMovement => Velocity.sqrMagnitude > Mathf.Epsilon;
    public bool IsGrounded => GroundContact.IsGrounded;

    public static SPlayerLocomotion Default => new SPlayerLocomotion(
        Vector3.zero,
        Vector3.zero,
        Vector3.forward,
        Vector2.zero,
        Vector2.zero,
        ELocomotionState.Idle,
        SGroundContact.None,
        0f,
        false,
        true);
}
