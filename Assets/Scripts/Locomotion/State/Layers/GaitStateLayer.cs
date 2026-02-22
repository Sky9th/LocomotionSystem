using UnityEngine;
using Game.Locomotion.State.Core;
using UnityEngine.InputSystem;

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

        public void Update(in SLocomotionStateContext context)
        {
            // No move input means the character should be considered idle.
            if (!context.MoveAction.Equals(SMoveIAction.None))
            {
                if(context.MoveAction.Phase == InputActionPhase.Canceled)
                {
                    Current = EMovementGait.Idle;
                } 
                else if (context.MoveAction.Phase == InputActionPhase.Performed)
                {
                    EMovementGait gait = Current;
                    if (gait == EMovementGait.Idle)
                    {
                        gait = EMovementGait.Run;
                    }

                    if (context.SprintAction.HasInput && context.SprintAction.Phase == InputActionPhase.Performed)
                    {
                        gait = gait == EMovementGait.Sprint ? EMovementGait.Run : EMovementGait.Sprint;
                    }

                    Current = gait;
                }
            }
        }
    }
}
