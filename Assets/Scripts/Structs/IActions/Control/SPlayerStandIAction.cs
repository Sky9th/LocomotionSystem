using System;
using UnityEngine;

/// <summary>
/// Canonical payload that represents aggregated look deltas (mouse/controller) for the player camera.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SPlayerStandIAction
{
    public SPlayerStandIAction(bool wantStand)
    {
        WantStand = wantStand;
    }

    /// <summary>
    /// Whether the player is issuing a stand intent this frame.
    /// </summary>
    public bool WantStand { get; }

    public bool HasInput => WantStand;

    public static SPlayerStandIAction None => new SPlayerStandIAction(false);
}
