using System;
using UnityEngine.InputSystem;

/// <summary>
/// Canonical payload that represents a prone (lie down) intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SProneIAction
{
    public SProneIAction(SButtonInputState button)
    {
        Button = button;
    }

    public SButtonInputState Button { get; }

    public SProneIAction ClearFrameSignals()
    {
        return new SProneIAction(Button.ClearFrameSignals());
    }

    public static SProneIAction CreateEvent(bool isPressed, InputActionPhase phase)
    {
        return new SProneIAction(SButtonInputState.CreateEvent(isPressed, phase));
    }

    public static SProneIAction None => new SProneIAction(SButtonInputState.None);
}
