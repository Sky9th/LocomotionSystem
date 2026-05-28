using System;
using UnityEngine.InputSystem;

namespace RedDust.Input
{
    [Serializable]
    public struct SIActionPrimaryInteract
    {
        public SIActionPrimaryInteract(SButtonInputState button)
        {
            Button = button;
        }

        public SButtonInputState Button { get; }

        public SIActionPrimaryInteract ClearFrameSignals()
        {
            return new SIActionPrimaryInteract(Button.ClearFrameSignals());
        }

        public static SIActionPrimaryInteract CreateEvent(bool isPressed, InputActionPhase phase)
        {
            return new SIActionPrimaryInteract(SButtonInputState.CreateEvent(isPressed, phase));
        }

        public static SIActionPrimaryInteract None => new SIActionPrimaryInteract(SButtonInputState.None);
    }
}
