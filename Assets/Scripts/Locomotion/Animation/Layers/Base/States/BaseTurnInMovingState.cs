using System.Diagnostics;
using Animancer;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Layers.Base.Conditions;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.Animation.Conditions;
using Game.Locomotion.State.Layers;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseTurnInMovingState : LocomotionLayerFsmState<BaseLayer>
    {
        private StringAsset selectedAlias;

        public BaseTurnInMovingState(BaseLayer owner) : base(owner)
        {
        }

        public override bool CanEnterState
        {
            get
            {
                var conditionContext = Owner.context;
                return conditionContext.Check<CanEnterTurnInMovingStateCondition>();
            }
        }

        public override bool CanExitState
        {
            get
            {
               return true;
            }
        }

        public override void OnEnterState()
        {
            selectedAlias = ResolveTurnAlias(Owner.AliasProfile, Owner.Snapshot.TurnAngle);
            Owner.Play(selectedAlias);
        }

        public override void OnExitState()
        {
            Logger.Log($"Exiting {nameof(BaseTurnInMovingState)}");
        }

        public override void Tick()
        {
            SLocomotion snapshot = Owner.Snapshot;

            if (Owner.TrySetState(BaseStateKey.Moving))
            {
                return;
            }

            if (Owner.TrySetState(BaseStateKey.Idle))
            {
                return;
            }

            if (Owner.HasCurrentAnimationCompleted())
            {
                if (snapshot.State == ELocomotionState.GroundedMoving)
                {
                    if (snapshot.IsTurning)
                    {
                        selectedAlias = ResolveTurnAlias(Owner.AliasProfile, snapshot.TurnAngle);
                        Owner.PlayFromStart(selectedAlias);
                        return;
                    }

                    Owner.ForceSetState(BaseStateKey.Moving);
                    return;
                }

                Owner.ForceSetState(BaseStateKey.Idle);
            }
        }

        private StringAsset ResolveTurnAlias(AnimancerStringProfile alias, float angle)
        {
            bool isRightTurn = angle > 0f;
            switch (Owner.Snapshot.Gait)
            {
                case EMovementGait.Walk:
                    return isRightTurn
                        ? alias.turnInWalk180R
                        : alias.turnInWalk180L;
                case EMovementGait.Run:
                    return isRightTurn
                        ? alias.turnInRun180R
                        : alias.turnInRun180L;
                case EMovementGait.Sprint:
                    return isRightTurn
                        ? alias.turnInSprint180R
                        : alias.turnInSprint180L;
                default:
                    Logger.LogWarning($"Unsupported gait {Owner.Snapshot.Gait} in turn resolver.");
                    return null;
            }
        }
    }
}
