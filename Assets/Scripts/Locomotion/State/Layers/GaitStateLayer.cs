using UnityEngine;
using Game.Locomotion.State.Core;

namespace Game.Locomotion.State.Layers
{
    /// <summary>
    /// Gait layer implementation that derives <see cref="EMovementGait"/>
    /// from high-level gait toggle inputs (Walk / Sprint) with Run as
    /// the default moving gait.
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
            // No move input means the character should be considered idle.
            if (!context.MoveAction.HasInput)
            {
                Current = EMovementGait.Idle;
                return;
            }

            // Start from the previous gait; if we are transitioning from
            // Idle into movement without any explicit gait input yet,
            // default to Run as the baseline moving gait.
            EMovementGait gait = Current;
            if (gait == EMovementGait.Idle)
            {
                gait = EMovementGait.Run;
            }

            // Walk and Sprint act as toggle buttons around the default
            // Run gait.
            if (context.WalkAction.HasInput)
            {
                gait = gait == EMovementGait.Walk ? EMovementGait.Run : EMovementGait.Walk;
            }

            if (context.SprintAction.HasInput)
            {
                gait = gait == EMovementGait.Sprint ? EMovementGait.Run : EMovementGait.Sprint;
            }

            // Optional: an explicit Run input can be used as a quick
            // way to reset back to the baseline running gait.
            if (context.RunAction.HasInput)
            {
                gait = EMovementGait.Run;
            }

            // Apply simple posture-based constraints so that prone cannot
            // move and crouch cannot sprint.
            EPostureState previousPosture = context.PreviousState.Posture;
            if (previousPosture == EPostureState.Prone)
            {
                gait = EMovementGait.Idle;
            }
            else if (previousPosture == EPostureState.Crouching && gait == EMovementGait.Sprint)
            {
                gait = EMovementGait.Run;
            }

            Current = gait;
        }
    }
}
