using System;
using UnityEngine;

/// <summary>
/// Canonical payload that represents a jump intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SSprintIAction
{
    public SSprintIAction(bool wantSprint)
    {
        WantSprint = wantSprint;
    }

    /// <summary>
    /// Whether the player is issuing a sprint intent this frame.
    /// </summary>
    public bool WantSprint { get; }

    public bool HasInput => WantSprint;

    public static SSprintIAction None => new SSprintIAction(false);
}
