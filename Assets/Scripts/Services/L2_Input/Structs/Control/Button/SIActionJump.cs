using System;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// Canonical payload that represents a jump intent from the player.
    /// Stored under Structs/IActions so every subsystem observes the same DTO layout.
    /// </summary>
    [Serializable]
    public struct SIActionJump
    {
        public SIActionJump(SButtonInputState button)
        {
            Button = button;
        }

        public SButtonInputState Button { get; }

        public SIActionJump ClearFrameSignals()
        {
            return new SIActionJump(Button.ClearFrameSignals());
        }

        public static SIActionJump CreateEvent(bool isPressed, InputActionPhase phase)
        {
            return new SIActionJump(SButtonInputState.CreateEvent(isPressed, phase));
        }

        public static SIActionJump None => new SIActionJump(SButtonInputState.None);
    }
}
