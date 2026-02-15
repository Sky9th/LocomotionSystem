using System;
using UnityEngine;

/// <summary>
/// Canonical payload that represents aggregated look deltas (mouse/controller) for the player camera.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SStandIAction
{
    public SStandIAction(bool wantStand)
    {
        WantStand = wantStand;
    }

    /// <summary>
    /// Whether the player is issuing a stand intent this frame.
    /// </summary>
    public bool WantStand { get; }

    public bool HasInput => WantStand;

    public static SStandIAction None => new SStandIAction(false);
}
