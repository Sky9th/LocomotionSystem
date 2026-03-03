using Game.Locomotion.Animation.Conditions;
using Game.Locomotion.Animation.Core;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers.Base.Conditions
{
    internal readonly struct CanExitTurnInPlaceByAngleCondition : ICheck<LocomotionAnimationContext>
    {
        public bool Evaluate(in LocomotionAnimationContext context)
            => Mathf.Abs(context.Snapshot.TurnAngle) < context.LocomotionProfile.turnExitAngle;
    }
}
