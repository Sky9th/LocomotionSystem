using System;
using UnityEngine;

namespace RedDust.Input
{
    /// <summary>
    /// Canonical payload that represents aggregated look deltas (mouse/controller) for the player camera.
    /// Stored under Structs/IActions so every subsystem observes the same DTO layout.
    /// </summary>
    [Serializable]
    public struct SIActionLook
    {
        public SIActionLook(Vector2 delta)
        {
            Delta = delta;
        }

        /// <summary>
        /// Raw look delta sampled this frame (X = yaw, Y = pitch).
        /// </summary>
        public Vector2 Delta { get; }

        public bool HasDelta => Delta.sqrMagnitude > Mathf.Epsilon;

        public static SIActionLook None => new SIActionLook(Vector2.zero);
    }
}
