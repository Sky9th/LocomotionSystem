using Animancer;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.Animation.Layers.Base.Conditions;
using Game.Locomotion.Animation.Conditions;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseIdleState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseIdleState(BaseLayer owner) : base(owner)
        {
        }

        public override bool CanEnterState
        {
            get
            {
                var conditionContext = Owner.context;
                return conditionContext.Check<CanEnterIdleStateCondition>();
            }
        }

        public override void OnEnterState()
        {
            Owner.Play(Owner.AliasProfile != null ? Owner.AliasProfile.idleL : null);
        }

        public override void Tick()
        {
            // Let the FSM drive its own transitions.
            if (Owner.TrySetState(BaseStateKey.TurnInPlace))
            {
                return;
            }

            if (Owner.TrySetState(BaseStateKey.IdleToMoving))
            {
                return;
            }

            if (Owner.TrySetState(BaseStateKey.Moving))
            {
                return;
            }

            StringAsset idleAlias = Owner.AliasProfile != null ? Owner.AliasProfile.idleL : null;
            Owner.PlayIfChanged(idleAlias);
        }
    }
}
