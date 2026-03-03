using Animancer;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.Animation.Layers.Base.Conditions;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseIdleState : LocomotionLayerFsmState<BaseLayerFsm>
    {
        public BaseIdleState(BaseLayerFsm owner) : base(owner)
        {
        }

        public override void OnEnterState()
        {
            Owner.Play(Owner.AliasProfile != null ? Owner.AliasProfile.idleL : null);
        }

        public override void Tick()
        {
            var conditionContext = Owner.context;

            // Let the FSM drive its own transitions.
            if (default(CanEnterIdleToMovingStateCondition).Evaluate(in conditionContext))
            {
                if (Owner.TrySetState(BaseStateKey.IdleToMoving))
                {
                    return;
                }
            }

            if (default(CanEnterTurnInPlaceStateCondition).Evaluate(in conditionContext))
            {
                if (Owner.TrySetState(BaseStateKey.TurnInPlace))
                {
                    return;
                }
            }

            StringAsset idleAlias = Owner.AliasProfile != null ? Owner.AliasProfile.idleL : null;
            Owner.PlayIfChanged(idleAlias);
        }
    }
}
