using UnityEngine;

namespace Game.Locomotion.Logic
{
    /// <summary>
    /// Minimal locomotion logic used in the early phase of Locomotion v2.
    ///
    /// Responsible only for translating basic move input into a
    /// movement gait and planar local velocity. High-level
    /// locomotion state resolution is handled by LocomotionStateLogic.
    /// </summary>
    internal static class BasicLocomotionLogic
    {
        internal static void EvaluateBasicLocomotion(
            SPlayerMoveIAction moveAction,
            out Vector2 localVelocity,
            out EMovementGait gait)
        {
            if (!moveAction.HasInput)
            {
                localVelocity = Vector2.zero;
                gait = EMovementGait.Idle;
                return;
            }

            localVelocity = moveAction.RawInput;
            gait = EMovementGait.Walk;
        }
    }
}
