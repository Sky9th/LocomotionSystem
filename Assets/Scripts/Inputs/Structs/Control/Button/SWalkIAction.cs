using System;
using UnityEngine.InputSystem;

/// <summary>
/// Canonical payload that represents a jump intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SWalkIAction
{
    public SWalkIAction(SButtonInputState button)
    {
        Button = button;
    }

    public SButtonInputState Button { get; }

    public SWalkIAction ClearFrameSignals()
    {
        return new SWalkIAction(Button.ClearFrameSignals());
    }

    public static SWalkIAction CreateEvent(bool isPressed, InputActionPhase phase)
    {
        return new SWalkIAction(SButtonInputState.CreateEvent(isPressed, phase));
    }

    public static SWalkIAction None => new SWalkIAction(SButtonInputState.None);
}
