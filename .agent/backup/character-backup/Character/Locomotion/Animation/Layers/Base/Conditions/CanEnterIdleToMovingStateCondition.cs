using Game.Locomotion.Animation.Conditions;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.Discrete.Aspects;

namespace Game.Locomotion.Animation.Layers.Base.Conditions
{
    internal readonly struct CanEnterIdleToMovingStateCondition : ICheck<LocomotionAnimationContext>
    {
        public bool Evaluate(in LocomotionAnimationContext context)
            => context.Snapshot.DiscreteState.Phase == ELocomotionPhase.GroundedMoving && context.Snapshot.DiscreteState.IsTurning;
    }
}
