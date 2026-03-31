using System;
using UnityEngine.InputSystem;

/// <summary>
/// Canonical payload that represents a jump intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SRunIAction
{
    public SRunIAction(SButtonInputState button)
    {
        Button = button;
    }

    public SButtonInputState Button { get; }

    public SRunIAction ClearFrameSignals()
    {
        return new SRunIAction(Button.ClearFrameSignals());
    }

    public static SRunIAction CreateEvent(bool isPressed, InputActionPhase phase)
    {
        return new SRunIAction(SButtonInputState.CreateEvent(isPressed, phase));
    }

    public static SRunIAction None => new SRunIAction(SButtonInputState.None);
}
