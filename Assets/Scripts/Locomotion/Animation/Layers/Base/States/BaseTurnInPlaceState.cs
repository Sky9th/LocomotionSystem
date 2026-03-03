using Animancer;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Layers.Base.Conditions;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.Animation.Conditions;
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
                var conditionContext = Owner.context;
                if (!conditionContext.Check<CanEnterTurnInPlaceStateCondition>())
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

                if (conditionContext.Check<CanEnterIdleToMovingStateCondition>())
                {
                    return true;
                }

                if (conditionContext.Check<CanEnterMovingStateCondition>())
                {
                    return true;
                }

                if (conditionContext.Check<CanExitTurnInPlaceByAngleCondition>())
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

            TurnAngleStepRotationApplier.TryApply(
                Owner.AnimationProfile,
                Owner.ModelRotator,
                in snapshot,
                Owner.DeltaTime);

            // Keep playing the selected alias while locked.
            if (selectedAlias != null)
            {
                Owner.PlayIfChanged(selectedAlias);
            }

            // Transition priority while turning in place:
            // 1) Started moving -> IdleToMoving
            // 2) Finished turning -> Idle
            if (Owner.TrySetState(BaseStateKey.IdleToMoving))
            {
                Logger.Log("Exiting turn since we started moving.");
                return;
            }

            if (Owner.TrySetState(BaseStateKey.Idle))
            {
                return;
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
