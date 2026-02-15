using System;
using UnityEngine;

/// <summary>
/// Canonical payload that represents a prone (lie down) intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SProneIAction
{
    public SProneIAction(bool wantProne)
    {
        WantProne = wantProne;
    }

    /// <summary>
    /// Whether the player is issuing a prone intent this frame.
    /// </summary>
    public bool WantProne { get; }

    public bool HasInput => WantProne;

    public static SProneIAction None => new SProneIAction(false);
}
