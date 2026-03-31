using System;
using UnityEngine.InputSystem;

/// <summary>
/// Canonical payload that represents a jump intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SJumpIAction
{
    public SJumpIAction(SButtonInputState button)
    {
        Button = button;
    }

    public SButtonInputState Button { get; }

    public SJumpIAction ClearFrameSignals()
    {
        return new SJumpIAction(Button.ClearFrameSignals());
    }

    public static SJumpIAction CreateEvent(bool isPressed, InputActionPhase phase)
    {
        return new SJumpIAction(SButtonInputState.CreateEvent(isPressed, phase));
    }

    public static SJumpIAction None => new SJumpIAction(SButtonInputState.None);
}
