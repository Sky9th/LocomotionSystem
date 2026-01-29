using System;
using UnityEngine;

/// <summary>
/// 描述角色当前与地面的接触情况。首版仅需 Idle/Walk 信息，可后续拓展。
/// </summary>
[Serializable]
public struct GroundContactStruct
{
    public GroundContactStruct(bool isGrounded, Vector3 point, Vector3 normal)
    {
        IsGrounded = isGrounded;
        ContactPoint = point;
        ContactNormal = normal;
    }

    public bool IsGrounded { get; }
    public Vector3 ContactPoint { get; }
    public Vector3 ContactNormal { get; }

    public static GroundContactStruct None => new GroundContactStruct(false, Vector3.zero, Vector3.up);
}
