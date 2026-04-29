using System.Diagnostics;
using Animancer;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Layers.Base.Conditions;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.Animation.Conditions;
using Game.Locomotion.Discrete.Aspects;
using Game.Locomotion.Config;
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
            selectedAlias = ResolveTurnAlias(Owner.AliasProfile, Owner.Snapshot.Motor.TurnAngle);
            Owner.Play(selectedAlias);
        }

        public override void OnExitState()
        {
            Logger.Log($"Exiting {nameof(BaseTurnInMovingState)}");
        }

        public override void Tick()
        {
            SCharacterSnapshot snapshot = Owner.Snapshot;

            if (!IsForwardOnlyIntent(snapshot.Motor.DesiredLocalVelocity, Owner.context.LocomotionProfile))
            {
                Owner.ForceSetState(BaseStateKey.Moving);
                return;
            }

            if (Owner.TrySetState(BaseStateKey.Moving))
            {
                return;
            }

            if (Owner.TrySetState(BaseStateKey.Idle))
            {
                return;
            }

            if (Owner.TrySetState(BaseStateKey.AirLoop))
            {
                return;
            }

            if (Owner.HasCurrentAnimationCompleted())
            {
                if (snapshot.DiscreteState.Phase == ELocomotionPhase.GroundedMoving)
                {
                    if (snapshot.DiscreteState.IsTurning)
                    {
                        selectedAlias = ResolveTurnAlias(Owner.AliasProfile, snapshot.Motor.TurnAngle);
                        Owner.PlayFromStart(selectedAlias);
                        return;
                    }

                    Owner.ForceSetState(BaseStateKey.Moving);
                    return;
                }

                Owner.ForceSetState(BaseStateKey.Idle);
            }
        }

        private static bool IsForwardOnlyIntent(Vector2 desiredLocalVelocity, LocomotionProfile locomotionProfile)
        {
            float moveSpeed = locomotionProfile != null ? locomotionProfile.moveSpeed : 0f;
            float forwardThreshold = moveSpeed > 0f ? moveSpeed * 0.9f : 0.01f;
            float lateralThreshold = moveSpeed > 0f ? moveSpeed * 0.1f : 0.01f;

            return desiredLocalVelocity.y >= forwardThreshold && Mathf.Abs(desiredLocalVelocity.x) <= lateralThreshold;
        }

        private StringAsset ResolveTurnAlias(LocomotionAliasProfile alias, float angle)
        {
            bool isRightTurn = angle > 0f;
            switch (Owner.Snapshot.DiscreteState.Gait)
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
                    Logger.LogWarning($"Unsupported gait {Owner.Snapshot.DiscreteState.Gait} in turn resolver.");
                    return null;
            }
        }
    }
}
