using Animancer;
using Game.Locomotion.Discrete.Aspects;
using Game.Locomotion.Config;
using Game.Locomotion.Animation.Layers.Base.Conditions;
using Game.Locomotion.Animation.Layers.Core;
using Game.Locomotion.Animation.Conditions;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseMovingState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseMovingState(BaseLayer owner) : base(owner)
        {
        }

        public override bool CanEnterState
        {
            get
            {
                var conditionContext = Owner.context;
                return conditionContext.Check<CanEnterMovingStateCondition>();
            }
        }

        public override void Tick()
        {
            if (Owner.TrySetState(BaseStateKey.TurnInMoving))
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

            StringAsset desired = ResolveMovingAlias(Owner.AliasProfile, Owner.Snapshot.DiscreteState.Gait);
            if (desired != null)
            {
                Owner.PlayIfChanged(desired);
            }

            UpdateMovementMixerParameterIfNeeded(Owner.LocomotionProfile);

            // If the player is turning while moving, keep it simple for now.
            // A dedicated TurnInMove state will be introduced later.
        }

        private void UpdateMovementMixerParameterIfNeeded(LocomotionProfile locomotionProfile)
        {
            if (locomotionProfile == null)
            {
                return;
            }

            if (Owner.AliasProfile == null)
            {
                return;
            }

            // Only drive parameters if the currently playing state is a 2D mixer.
            if (Owner.Layer == null)
            {
                return;
            }

            // BaseLayerFsm keeps the current AnimancerState internally, but we can still
            // drive the mixer parameter via the currently playing state on the layer.
            // This assumes the base locomotion mixer is the layer's current state.
            if (Owner.Layer.CurrentState is not Vector2MixerState vector2Mixer)
            {
                return;
            }

            float maxMoveSpeed = locomotionProfile.moveSpeed;
            if (maxMoveSpeed <= 0f)
            {
                return;
            }

            SCharacterSnapshot snapshot = Owner.Snapshot;

            Vector2 planarVelocity = snapshot.Motor.ActualLocalVelocity;
            Vector2 parameter = planarVelocity / maxMoveSpeed;
            if (parameter.sqrMagnitude > 1f)
            {
                parameter.Normalize();
            }

            vector2Mixer.Parameter = parameter;

            TurnAngleStepRotationApplier.TryApply(
                Owner.AnimationProfile,
                Owner.Transformer,
                in snapshot,
                Owner.DeltaTime);
        }

        private static StringAsset ResolveMovingAlias(LocomotionAliasProfile aliasProfile, EMovementGait gait)
        {
            if (aliasProfile == null)
            {
                return null;
            }

            return gait switch
            {
                EMovementGait.Walk => aliasProfile.walkMixer,
                EMovementGait.Run => aliasProfile.runMixer,
                EMovementGait.Sprint => aliasProfile.sprint,
                _ => aliasProfile.walkMixer,
            };
        }
    }
}
