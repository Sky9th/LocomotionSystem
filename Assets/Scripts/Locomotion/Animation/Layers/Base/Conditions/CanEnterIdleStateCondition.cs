using Game.Locomotion.Animation.Conditions;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.State.Layers;

namespace Game.Locomotion.Animation.Layers.Base.Conditions
{
    internal readonly struct CanEnterIdleStateCondition : ICheck<LocomotionAnimationContext>
    {
        public bool Evaluate(in LocomotionAnimationContext context)
            => context.Snapshot.State == ELocomotionState.GroundedIdle;
    }
}
