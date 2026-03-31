using UnityEngine;
using Game.Locomotion.Discrete.Interface;
using UnityEngine.InputSystem;
using Game.Locomotion.Input;

namespace Game.Locomotion.Discrete.Aspects
{
    /// <summary>
    /// Gait layer implementation that derives <see cref="EMovementGait"/>
    /// from high-level gait toggle inputs (Walk / Sprint) with Run as
    /// the default moving gait.
    /// </summary>
    internal sealed class GaitAspect : ILocomotionAspect<EMovementGait>
    {
        public EMovementGait Current { get; private set; } = EMovementGait.Idle;

        public void Reset(EMovementGait defaultState)
        {
            Current = defaultState;
        }

        public void Update(in SLocomotionMotor agent, in SLocomotionInputActions actions)
        {
            // No move input means the character should be considered idle.
            if (!actions.MoveAction.Equals(SMoveIAction.None))
            {
                if(actions.MoveAction.Phase == InputActionPhase.Canceled)
                {
                    Current = EMovementGait.Idle;
                } 
                else if (actions.MoveAction.Phase == InputActionPhase.Performed)
                {
                    EMovementGait gait = Current;
                    if (gait == EMovementGait.Idle)
                    {
                        gait = EMovementGait.Run;
                    }

                    if (actions.SprintAction.Button.IsRequested)
                    {
                        gait = gait == EMovementGait.Sprint ? EMovementGait.Run : EMovementGait.Sprint;
                    }

                    Current = gait;
                }
            }
        }
    }
}
