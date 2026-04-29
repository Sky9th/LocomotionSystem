using Game.Locomotion.Animation.Conditions;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.Discrete.Aspects;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers.Base.Conditions
{
    internal readonly struct CanEnterTurnInMovingStateCondition : ICheck<LocomotionAnimationContext>
    {
        public bool Evaluate(in LocomotionAnimationContext context)
        {
            if (context.Snapshot.DiscreteState.Phase != ELocomotionPhase.GroundedMoving)
            {
                return false;
            }

            if (!context.Snapshot.DiscreteState.IsTurning)
            {
                return false;
            }

            // This state should only be valid when the player intends to move forward (holding W).
            // We derive intent from DesiredLocalVelocity (computed from MoveAction).
            Vector2 desired = context.Snapshot.Motor.DesiredLocalVelocity;

            float moveSpeed = context.LocomotionProfile != null ? context.LocomotionProfile.moveSpeed : 0f;
            float forwardThreshold = moveSpeed > 0f ? moveSpeed * 0.9f : 0.01f;
            float lateralThreshold = moveSpeed > 0f ? moveSpeed * 0.1f : 0.01f;

            return desired.y >= forwardThreshold && Mathf.Abs(desired.x) <= lateralThreshold;
        }
    }
}
