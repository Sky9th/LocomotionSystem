using System;
using UnityEngine.InputSystem;

namespace RedDust.Input
{
    /// <summary>
    /// Canonical payload that represents a prone (lie down) intent from the player.
    /// Stored under Structs/IActions so every subsystem observes the same DTO layout.
    /// </summary>
    [Serializable]
    public struct SIActionProne
    {
        public SIActionProne(SButtonInputState button)
        {
            Button = button;
        }

        public SButtonInputState Button { get; }

        public SIActionProne ClearFrameSignals()
        {
            return new SIActionProne(Button.ClearFrameSignals());
        }

        public static SIActionProne CreateEvent(bool isPressed, InputActionPhase phase)
        {
            return new SIActionProne(SButtonInputState.CreateEvent(isPressed, phase));
        }

        public static SIActionProne None => new SIActionProne(SButtonInputState.None);
    }
}
