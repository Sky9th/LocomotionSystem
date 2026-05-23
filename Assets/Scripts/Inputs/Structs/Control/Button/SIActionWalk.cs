using System;
using UnityEngine.InputSystem;

/// <summary>
/// Canonical payload that represents a jump intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SIActionWalk
{
    public SIActionWalk(SButtonInputState button)
    {
        Button = button;
    }

    public SButtonInputState Button { get; }

    public SIActionWalk ClearFrameSignals()
    {
        return new SIActionWalk(Button.ClearFrameSignals());
    }

    public static SIActionWalk CreateEvent(bool isPressed, InputActionPhase phase)
    {
        return new SIActionWalk(SButtonInputState.CreateEvent(isPressed, phase));
    }

    public static SIActionWalk None => new SIActionWalk(SButtonInputState.None);
}
