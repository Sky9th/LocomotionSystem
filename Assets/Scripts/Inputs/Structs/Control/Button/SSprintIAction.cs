using System;
using UnityEngine.InputSystem;

/// <summary>
/// Canonical payload that represents a sprint intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SSprintIAction
{

    public SSprintIAction(SButtonInputState button)
    {
        Button = button;
    }

    public SButtonInputState Button { get; }

    public SSprintIAction ClearFrameSignals()
    {
        return new SSprintIAction(Button.ClearFrameSignals());
    }

    public static SSprintIAction CreateEvent(bool isPressed, InputActionPhase phase)
    {
        return new SSprintIAction(SButtonInputState.CreateEvent(isPressed, phase));
    }

    public static SSprintIAction None => new SSprintIAction(SButtonInputState.None);
}
