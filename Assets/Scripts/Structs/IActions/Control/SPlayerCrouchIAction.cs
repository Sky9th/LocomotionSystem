using System;
using UnityEngine;

/// <summary>
/// Canonical payload that represents aggregated look deltas (mouse/controller) for the player camera.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SPlayerCrouchIAction
{
    public SPlayerCrouchIAction(bool wantCrouch)
    {
        WantCrouch = wantCrouch;
    }

    /// <summary>
    /// Whether the player is issuing a crouch intent this frame.
    /// </summary>
    public bool WantCrouch { get; }

    public bool HasInput => WantCrouch;

    public static SPlayerCrouchIAction None => new SPlayerCrouchIAction(false);
}
