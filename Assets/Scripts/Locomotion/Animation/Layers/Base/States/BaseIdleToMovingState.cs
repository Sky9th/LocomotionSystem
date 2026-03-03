using Game.Locomotion.Animation.Layers.Base.Conditions;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.State.Layers;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseIdleToMovingState : LocomotionLayerFsmState<BaseLayerFsm>
    {
        public BaseIdleToMovingState(BaseLayerFsm owner) : base(owner)
        {
        }

        public override void OnEnterState()
        {
            if (Owner.Snapshot.TurnAngle > 0)
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
            // Ensure the next state starts with the correct animation if we were interrupted.
            Logger.Log($"IdleToMoving state exiting. Interrupted: {!Owner.HasCurrentAnimationCompleted()}");
        }

        public override void Tick()
        {
            var conditionContext = Owner.context;

            if (conditionContext.Snapshot.State == ELocomotionState.GroundedIdle)
            {
                Owner.TrySetState(BaseStateKey.Idle);
                return;
            }

            if (default(CanEnterMovingStateCondition).Evaluate(in conditionContext))
            {
                Owner.TrySetState(BaseStateKey.Moving);
                return;
            }
            
            if (Owner.HasCurrentAnimationCompleted())
            {
                Owner.TrySetState(BaseStateKey.Moving);
                return;
            }
        }

        public override bool CanEnterState
        {
            get
            {
                var conditionContext = Owner.context;
                return default(CanEnterIdleToMovingStateCondition).Evaluate(in conditionContext);
            }
        }

        public override bool CanExitState
        {
            get
            {
                var conditionContext = Owner.context;
                return default(CanExitIdleToMovingStateCondition).Evaluate(in conditionContext);
            }
        }
    }
}
