using UnityEngine;

namespace Game.Locomotion.LegacyControl
{
    /// <summary>
    /// Read-only input snapshot passed into locomotion controllers
    /// and state machines. Keeps the control layer decoupled from
    /// MonoBehaviour and other game-specific details.
    /// </summary>
    internal readonly struct LocomotionStateContext
    {
        public readonly Vector3 Velocity;
        public readonly SGroundContact GroundContact;
        public readonly LocomotionConfigProfile Config;

        public LocomotionStateContext(
            Vector3 velocity,
            SGroundContact groundContact,
            LocomotionConfigProfile config)
        {
            Velocity = velocity;
            GroundContact = groundContact;
            Config = config;
        }
    }
}
