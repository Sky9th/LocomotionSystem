using System;
using UnityEngine;

/// <summary>
/// Describes the current ground contact state for the character.
/// </summary>
[Serializable]
public struct SGroundContact
{
    public SGroundContact(bool isGrounded, Vector3 point, Vector3 normal)
    {
        IsGrounded = isGrounded;
        ContactPoint = point;
        ContactNormal = normal;
    }

    public bool IsGrounded { get; }
    public Vector3 ContactPoint { get; }
    public Vector3 ContactNormal { get; }

    public static SGroundContact None => new SGroundContact(false, Vector3.zero, Vector3.up);
}
