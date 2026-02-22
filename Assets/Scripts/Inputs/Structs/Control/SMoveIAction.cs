using System;
using UnityEngine;

/// <summary>
/// Canonical payload that represents planar locomotion intent in world space.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SMoveIAction
{
    public SMoveIAction(Vector2 rawInput, Vector3 worldDirection)
    {
        RawInput = rawInput;
        WorldDirection = worldDirection;
    }

    public Vector2 RawInput { get; }
    public Vector3 WorldDirection { get; }

    public bool HasInput => RawInput.sqrMagnitude > Mathf.Epsilon;

    public static SMoveIAction None => new SMoveIAction(
        Vector2.zero,
        Vector3.zero);
}
