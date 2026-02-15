using System;
using UnityEngine;

/// <summary>
/// Canonical payload that represents a jump intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SWalkIAction
{
    public SWalkIAction(bool wantWalk)
    {
        WantWalk = wantWalk;
    }

    /// <summary>
    /// Whether the player is issuing a walk intent this frame.
    /// </summary>
    public bool WantWalk { get; }

    public bool HasInput => WantWalk;

    public static SWalkIAction None => new SWalkIAction(false);
}
