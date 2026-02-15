using System;
using UnityEngine;

/// <summary>
/// Canonical payload that represents a jump intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SRunIAction
{
    public SRunIAction(bool wantRun)
    {
        WantRun = wantRun;
    }

    /// <summary>
    /// Whether the player is issuing a run intent this frame.
    /// </summary>
    public bool WantRun { get; }

    public bool HasInput => WantRun;

    public static SRunIAction None => new SRunIAction(false);
}
