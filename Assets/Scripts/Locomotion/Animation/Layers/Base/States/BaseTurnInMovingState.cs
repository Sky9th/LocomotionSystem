using System.Diagnostics;
using Animancer;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.State.Layers;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseTurnInMovingState : BaseLayerFsmState
    {
        private StringAsset selectedAlias;

        public BaseTurnInMovingState(BaseLayerFsm owner) : base(owner)
        {
        }

        public override bool CanEnterState
        {
            get
            {
                return true;
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
            if (Owner.Snapshot.State != ELocomotionState.GroundedMoving && !Owner.Snapshot.IsTurning)
            {
                Owner.TrySetState(BaseStateKey.Moving);
                return;
            }

            if (Owner.Snapshot.State != ELocomotionState.GroundedIdle && !Owner.Snapshot.IsTurning)
            {
                Owner.TrySetState(BaseStateKey.Idle);
                return;
            }

            if (Owner.HasCurrentAnimationCompleted())
            {
                if (Owner.Snapshot.State == ELocomotionState.GroundedMoving)
                {
                    Owner.TrySetState(BaseStateKey.Moving);
                }
                else
                {
                    Owner.TrySetState(BaseStateKey.Idle);
                }
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
