using UnityEngine;
using Game.Locomotion.State.Core;

namespace Game.Locomotion.State.Layers
{
    /// <summary>
    /// Gait layer implementation that maps planar speed to an <see cref="EMovementGait"/>.
    /// </summary>
    internal sealed class GaitStateLayer : ILocomotionStateLayer<EMovementGait>
    {
        public EMovementGait Current { get; private set; } = EMovementGait.Idle;

        public void Reset(EMovementGait defaultState)
        {
            Current = defaultState;
        }

        public void Update(in LocomotionStateContext context)
        {
            Vector3 velocity = context.Velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;

            if (speed <= Mathf.Epsilon)
            {
                Current = EMovementGait.Idle;
                return;
            }

            LocomotionConfigProfile config = context.Config;
            if (config == null)
            {
                Current = speed < 1.0f ? EMovementGait.Walk : EMovementGait.Run;
                return;
            }

            float maxSpeed = Mathf.Max(config.MoveSpeed, 0.01f);
            float walkThreshold = maxSpeed * 0.4f;
            float runThreshold = maxSpeed * 0.8f;

            if (speed < walkThreshold)
            {
                Current = EMovementGait.Walk;
            }
            else if (speed < runThreshold)
            {
                Current = EMovementGait.Run;
            }
            else
            {
                Current = EMovementGait.Sprint;
            }
        }
    }
}
