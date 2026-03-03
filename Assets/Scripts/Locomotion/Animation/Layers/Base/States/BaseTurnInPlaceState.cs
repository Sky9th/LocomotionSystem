using Animancer;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Layers.Base.Conditions;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.State.Layers;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseTurnInPlaceState : LocomotionLayerFsmState<BaseLayerFsm>
    {
        private StringAsset selectedAlias;

        public BaseTurnInPlaceState(BaseLayerFsm owner) : base(owner)
        {
        }

        public override bool CanEnterState
        {
            get
            {
                if (Owner.AliasProfile == null)
                {
                    return false;
                }

                SLocomotion snapshot = Owner.Snapshot;
                var conditionContext = Owner.context;
                if (!default(CanEnterTurnInPlaceStateCondition).Evaluate(in conditionContext))
                {
                    return false;
                }

                return true;
            }
        }

        public override bool CanExitState
        {
            get
            {
                // Allow leaving the turn segment if the animation is complete
                // or if higher-level logic indicates we started moving.
                var conditionContext = Owner.context;

                if (default(CanEnterMovingStateCondition).Evaluate(in conditionContext))
                {
                    return true;
                }

                if (default(CanExitTurnInPlaceByAngleCondition).Evaluate(in conditionContext))
                {
                    Logger.Log($"Allowing early exit from turn since turn angle {Owner.Snapshot.TurnAngle} is below exit threshold.");
                    return true;
                }

                return Owner.HasCurrentAnimationCompleted();
            }
        }

        public override void OnEnterState()
        {
            selectedAlias = ResolveTurnAlias(Owner.AliasProfile, Owner.Snapshot.TurnAngle);
            Owner.Play(selectedAlias);
        }

        public override void OnExitState()
        {
            Logger.Log($"Exiting {nameof(BaseTurnInPlaceState)}");
        }

        public override void Tick()
        {
            SLocomotion snapshot = Owner.Snapshot;
            var conditionContext = Owner.context;

            // If we started moving, exit immediately (the owning layer will
            // pick the correct locomotion animation for movement).
            if (default(CanEnterMovingStateCondition).Evaluate(in conditionContext))
            {
                Logger.Log("Exiting turn since we started moving.");
                Owner.TrySetState(BaseStateKey.IdleToMoving);
                return;
            }

            if (default(CanExitTurnInPlaceByTurnFlagCondition).Evaluate(in conditionContext))
            {
                Owner.TrySetState(BaseStateKey.Idle);
                return;
            }

            TurnAngleStepRotationApplier.TryApply(
                Owner.AnimationProfile,
                Owner.ModelRotator,
                in snapshot,
                Owner.DeltaTime);

            // Once the turn clip finishes, exit back to idle.
            if (Owner.HasCurrentAnimationCompleted())
            {
                Owner.TrySetState(BaseStateKey.Idle);
                return;
            }

            // Keep playing the selected alias while locked.
            if (selectedAlias != null)
            {
                Owner.PlayIfChanged(selectedAlias);
            }
        }

        private static StringAsset ResolveTurnAlias(AnimancerStringProfile alias, float angle)
        {
            if (alias == null)
            {
                return null;
            }

            bool isRightTurn = angle > 0f;

            if (isRightTurn)
            {
                return alias.turnInPlace90R;
            }

            return alias.turnInPlace90L;
        }
    }
}
