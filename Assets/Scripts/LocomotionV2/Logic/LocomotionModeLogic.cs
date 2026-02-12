using UnityEngine;

namespace Game.Locomotion.Logic
{
    /// <summary>
    /// Aggregates discrete locomotion state (state, posture, gait, condition)
    /// and minimal local velocity into a single evaluation step.
    /// </summary>
    internal static class LocomotionModeLogic
    {
        internal static void Evaluate(
            Vector3 velocity,
            SGroundContact groundContact,
            SPlayerMoveIAction moveAction,
            out Vector2 localVelocity,
            out SLocomotionDiscreteState discreteState)
        {
            // Derive local planar velocity and gait from input only.
            BasicLocomotionLogic.EvaluateBasicLocomotion(moveAction, out localVelocity, out EMovementGait gait);

            // Derive high-level locomotion state from velocity and ground contact.
            ELocomotionState state = LocomotionStateLogic.ResolveHighLevelState(velocity, groundContact);

            // Posture and condition are placeholders for now.
            EPostureState posture = EPostureState.Standing;
            ELocomotionCondition condition = ELocomotionCondition.Normal;

            discreteState = new SLocomotionDiscreteState(state, posture, gait, condition);
        }
    }
}
