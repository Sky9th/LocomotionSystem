using System;
using UnityEngine.InputSystem;

namespace RedDust.Input
{
    /// <summary>
    /// Canonical payload that represents a stand intent from the player.
    /// Stored under Structs/IActions so every subsystem observes the same DTO layout.
    /// </summary>
    [Serializable]
    public struct SIActionStand
    {
        public SIActionStand(SButtonInputState button)
        {
            Button = button;
        }

        public SButtonInputState Button { get; }

        public SIActionStand ClearFrameSignals()
        {
            return new SIActionStand(Button.ClearFrameSignals());
        }

        public static SIActionStand CreateEvent(bool isPressed, InputActionPhase phase)
        {
            return new SIActionStand(SButtonInputState.CreateEvent(isPressed, phase));
        }

        public static SIActionStand None => new SIActionStand(SButtonInputState.None);
    }
}
