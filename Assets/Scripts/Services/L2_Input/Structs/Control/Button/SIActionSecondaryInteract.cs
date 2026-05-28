using System;
using UnityEngine.InputSystem;

[Serializable]
public struct SIActionSecondaryInteract
{
    public SIActionSecondaryInteract(SButtonInputState button)
    {
        Button = button;
    }

    public SButtonInputState Button { get; }

    public SIActionSecondaryInteract ClearFrameSignals()
    {
        return new SIActionSecondaryInteract(Button.ClearFrameSignals());
    }

    public static SIActionSecondaryInteract CreateEvent(bool isPressed, InputActionPhase phase)
    {
        return new SIActionSecondaryInteract(SButtonInputState.CreateEvent(isPressed, phase));
    }

    public static SIActionSecondaryInteract None => new SIActionSecondaryInteract(SButtonInputState.None);
}
