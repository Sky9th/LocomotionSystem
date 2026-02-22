using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Canonical payload that represents a sprint intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SSprintIAction
{

    public SSprintIAction(bool hasInput, InputActionPhase phase)
    {
        HasInput = hasInput;
        Phase = phase;
    }

    /// <summary>
    /// Discrete phase for this particular sprint input event (pressed / released).
    /// </summary>
    public InputActionPhase Phase { get; }

    public bool HasInput {get; set;}

    public static SSprintIAction None => new SSprintIAction(false, InputActionPhase.Disabled);
}
