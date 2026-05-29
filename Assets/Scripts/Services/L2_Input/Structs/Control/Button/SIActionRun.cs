using System;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// Canonical payload that represents a jump intent from the player.
    /// Stored under Structs/IActions so every subsystem observes the same DTO layout.
    /// </summary>
    [Serializable]
    public struct SIActionRun
    {
        public SIActionRun(SButtonInputState button)
        {
            Button = button;
        }

        public SButtonInputState Button { get; }

        public SIActionRun ClearFrameSignals()
        {
            return new SIActionRun(Button.ClearFrameSignals());
        }

        public static SIActionRun CreateEvent(bool isPressed, InputActionPhase phase)
        {
            return new SIActionRun(SButtonInputState.CreateEvent(isPressed, phase));
        }

        public static SIActionRun None => new SIActionRun(SButtonInputState.None);
    }
}
