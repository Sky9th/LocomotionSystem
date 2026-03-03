using Game.Locomotion.Animation.Conditions;
using Game.Locomotion.Animation.Core;

namespace Game.Locomotion.Animation.Layers.Base.Conditions
{
    internal readonly struct CanExitIdleToMovingStateCondition : ICheck<LocomotionAnimationContext>
    {
        public bool Evaluate(in LocomotionAnimationContext context)
            => !context.Snapshot.IsTurning;
    }
}
