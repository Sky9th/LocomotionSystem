using Game.Locomotion.Animation.Layers.Base.Conditions;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.Animation.Conditions;
using Game.Locomotion.Discrete.Aspects;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseIdleToMovingState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseIdleToMovingState(BaseLayer owner) : base(owner)
        {
        }

        public override void OnEnterState()
        {
            if (Owner.Snapshot.Motor.TurnAngle > 0)
            {
                Owner.Play(Owner.AliasProfile.idleToRun180R);
            } 
            else
            {
                Owner.Play(Owner.AliasProfile.idleToRun180L);
            }
        }

        public override void OnExitState()
        {
        }

        public override void Tick()
        {
            if (Owner.TrySetState(BaseStateKey.Idle))
            {
                return;
            }

            if (Owner.TrySetState(BaseStateKey.Moving))
            {
                return;
            }

            if (Owner.TrySetState(BaseStateKey.AirLoop))
            {
                return;
            }
            
            if (Owner.HasCurrentAnimationCompleted())
            {
                if (Owner.Snapshot.DiscreteState.Phase == ELocomotionPhase.GroundedMoving)
                {
                    Owner.ForceSetState(BaseStateKey.Moving);
                }
                else
                {
                    Owner.ForceSetState(BaseStateKey.Idle);
                }
                return;
            }
        }

        public override bool CanEnterState
        {
            get
            {
                var conditionContext = Owner.context;
                return conditionContext.Check<CanEnterIdleToMovingStateCondition>();
            }
        }

        public override bool CanExitState
        {
            get
            {
                var conditionContext = Owner.context;
                return conditionContext.Check<CanExitIdleToMovingStateCondition>();
            }
        }
    }
}
