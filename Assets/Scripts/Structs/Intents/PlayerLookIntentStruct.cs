using System;
using UnityEngine;

/// <summary>
/// Canonical payload that represents aggregated look deltas (mouse/controller) for the player camera.
/// Stored under Structs/Intents so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct PlayerLookIntentStruct
{
    public PlayerLookIntentStruct(Vector2 delta)
    {
        Delta = delta;
    }

    /// <summary>
    /// Raw look delta sampled this frame (X = yaw, Y = pitch).
    /// </summary>
    public Vector2 Delta { get; }

    public bool HasDelta => Delta.sqrMagnitude > Mathf.Epsilon;

    public static PlayerLookIntentStruct None => new PlayerLookIntentStruct(Vector2.zero);
}
