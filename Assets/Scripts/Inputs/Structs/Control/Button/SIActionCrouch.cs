using System;
using UnityEngine.InputSystem;

/// <summary>
/// Canonical payload that represents aggregated look deltas (mouse/controller) for the player camera.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SIActionCrouch
{
    public SIActionCrouch(SButtonInputState button)
    {
        Button = button;
    }

    public SButtonInputState Button { get; }

    public SIActionCrouch ClearFrameSignals()
    {
        return new SIActionCrouch(Button.ClearFrameSignals());
    }

    public static SIActionCrouch CreateEvent(bool isPressed, InputActionPhase phase)
    {
        return new SIActionCrouch(SButtonInputState.CreateEvent(isPressed, phase));
    }

    public static SIActionCrouch None => new SIActionCrouch(SButtonInputState.None);
}
