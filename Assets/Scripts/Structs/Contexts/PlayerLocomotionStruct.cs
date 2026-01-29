using System;
using UnityEngine;

/// <summary>
/// 角色当前的移动快照。首版聚焦 Idle/Walk，所以只存储位置、速度、朝向与地面信息。
/// </summary>
[Serializable]
public struct PlayerLocomotionStruct
{
    public PlayerLocomotionStruct(
        Vector3 position,
        Vector3 velocity,
        Vector3 forward,
        PlayerLocomotionState state,
        GroundContactStruct groundContact)
    {
        Position = position;
        Velocity = velocity;
        Forward = forward.sqrMagnitude > Mathf.Epsilon ? forward.normalized : Vector3.forward;
        State = state;
        GroundContact = groundContact;
    }

    public Vector3 Position { get; }
    public Vector3 Velocity { get; }
    public Vector3 Forward { get; }
    public PlayerLocomotionState State { get; }
    public GroundContactStruct GroundContact { get; }

    public float Speed => Velocity.magnitude;
    public bool HasMovement => Velocity.sqrMagnitude > Mathf.Epsilon;
    public bool IsGrounded => GroundContact.IsGrounded;

    public static PlayerLocomotionStruct Default => new PlayerLocomotionStruct(
        Vector3.zero,
        Vector3.zero,
        Vector3.forward,
        PlayerLocomotionState.Idle,
        GroundContactStruct.None);
}
