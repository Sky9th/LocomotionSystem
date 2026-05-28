using System;
using UnityEngine.InputSystem;

namespace RedDust.Input
{
    /// <summary>
    /// Canonical payload that represents a sprint intent from the player.
    /// Stored under Structs/IActions so every subsystem observes the same DTO layout.
    /// </summary>
    [Serializable]
    public struct SIActionSprint
    {

        public SIActionSprint(SButtonInputState button)
        {
            Button = button;
        }

        public SButtonInputState Button { get; }

        public SIActionSprint ClearFrameSignals()
        {
            return new SIActionSprint(Button.ClearFrameSignals());
        }

        public static SIActionSprint CreateEvent(bool isPressed, InputActionPhase phase)
        {
            return new SIActionSprint(SButtonInputState.CreateEvent(isPressed, phase));
        }

        public static SIActionSprint None => new SIActionSprint(SButtonInputState.None);
    }
}
