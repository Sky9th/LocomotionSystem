using System;
using UnityEngine;

/// <summary>
/// Canonical payload that represents a jump intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SJumpIAction
{
    public SJumpIAction(bool wantJump)
    {
        WantJump = wantJump;
    }

    /// <summary>
    /// Whether the player is issuing a jump intent this frame.
    /// </summary>
    public bool WantJump { get; }

    public bool HasInput => WantJump;

    public static SJumpIAction None => new SJumpIAction(false);
}
