using UnityEngine;

namespace Game.Locomotion.LegacyControl
{
    /// <summary>
    /// Resolves movement gait (Idle/Walk/Run/Sprint) from current
    /// velocity and shared locomotion configuration.
    /// </summary>
    internal static class LocomotionGaitResolver
    {
        internal static EMovementGait ResolveMovementGait(Vector3 velocity, LocomotionConfigProfile config)
        {
            float speed = velocity.magnitude;
            if (speed <= Mathf.Epsilon)
            {
                return EMovementGait.Idle;
            }

            if (config == null)
            {
                return EMovementGait.Walk;
            }

            float maxSpeed = Mathf.Max(config.MoveSpeed, 0.01f);
            float walkThreshold = maxSpeed * 0.4f;
            float runThreshold = maxSpeed * 0.8f;

            if (speed < walkThreshold)
            {
                return EMovementGait.Walk;
            }

            if (speed < runThreshold)
            {
                return EMovementGait.Run;
            }

            return EMovementGait.Sprint;
        }
    }
}
