using System;
using UnityEngine.InputSystem;

/// <summary>
/// Canonical payload that represents aggregated look deltas (mouse/controller) for the player camera.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SCrouchIAction
{
    public SCrouchIAction(SButtonInputState button)
    {
        Button = button;
    }

    public SButtonInputState Button { get; }

    public SCrouchIAction ClearFrameSignals()
    {
        return new SCrouchIAction(Button.ClearFrameSignals());
    }

    public static SCrouchIAction CreateEvent(bool isPressed, InputActionPhase phase)
    {
        return new SCrouchIAction(SButtonInputState.CreateEvent(isPressed, phase));
    }

    public static SCrouchIAction None => new SCrouchIAction(SButtonInputState.None);
}
