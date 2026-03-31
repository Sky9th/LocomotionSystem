using System;
using UnityEngine.InputSystem;

/// <summary>
/// Shared state model for button-like input actions.
/// Separates held state from one-frame request and release signals.
/// </summary>
[Serializable]
public readonly struct SButtonInputState
{
    public SButtonInputState(bool isPressed, InputActionPhase phase, bool isRequested, bool isReleased)
    {
        IsPressed = isPressed;
        Phase = phase;
        IsRequested = isRequested;
        IsReleased = isReleased;
    }

    public bool IsPressed { get; }
    public InputActionPhase Phase { get; }
    public bool IsRequested { get; }
    public bool IsReleased { get; }

    public SButtonInputState ClearFrameSignals()
    {
        return new SButtonInputState(IsPressed, Phase, isRequested: false, isReleased: false);
    }

    public static SButtonInputState CreateEvent(bool isPressed, InputActionPhase phase)
    {
        return new SButtonInputState(
            isPressed,
            phase,
            isRequested: isPressed && phase == InputActionPhase.Performed,
            isReleased: !isPressed && phase == InputActionPhase.Canceled);
    }

    public static SButtonInputState None => new SButtonInputState(false, InputActionPhase.Waiting, false, false);
}