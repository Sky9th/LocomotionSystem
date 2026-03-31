using System;
using UnityEngine.InputSystem;

/// <summary>
/// Canonical payload that represents a stand intent from the player.
/// Stored under Structs/IActions so every subsystem observes the same DTO layout.
/// </summary>
[Serializable]
public struct SStandIAction
{
    public SStandIAction(SButtonInputState button)
    {
        Button = button;
    }

    public SButtonInputState Button { get; }

    public SStandIAction ClearFrameSignals()
    {
        return new SStandIAction(Button.ClearFrameSignals());
    }

    public static SStandIAction CreateEvent(bool isPressed, InputActionPhase phase)
    {
        return new SStandIAction(SButtonInputState.CreateEvent(isPressed, phase));
    }

    public static SStandIAction None => new SStandIAction(SButtonInputState.None);
}
