using Animancer;
using Game.Locomotion.State.Layers;
using Game.Locomotion.Config;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal sealed class BaseMovingState : BaseLayerFsmState
    {
        public BaseMovingState(BaseLayerFsm owner) : base(owner)
        {
        }

        public override bool CanEnterState => Owner.Snapshot.State == ELocomotionState.GroundedMoving;

        public override void Tick()
        {
            if (Owner.Snapshot.State != ELocomotionState.GroundedMoving)
            {
                Owner.TrySetState(BaseStateKey.Idle);
                return;
            }

            if (Owner.Snapshot.IsTurning)
            {
                Owner.TrySetState(BaseStateKey.TurnInMoving);
                return;
            } 

            StringAsset desired = ResolveMovingAlias(Owner.AliasProfile, Owner.Snapshot.Gait);
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

            SLocomotion snapshot = Owner.Snapshot;

            Vector2 planarVelocity = snapshot.ActualLocalVelocity;
            Vector2 parameter = planarVelocity / maxMoveSpeed;
            if (parameter.sqrMagnitude > 1f)
            {
                parameter.Normalize();
            }

            vector2Mixer.Parameter = parameter;

            var animationProfile = Owner.AnimationProfile;
            var modelRotator = Owner.ModelRotator;
            if (animationProfile != null && modelRotator != null)
            {
                bool isMoving = snapshot.Gait != EMovementGait.Idle;
                float turnSpeed = animationProfile.GetTurnSpeed(snapshot.Posture, snapshot.Gait, isMoving);
                float absAngle = Mathf.Abs(snapshot.TurnAngle);
                if (turnSpeed > 0f && absAngle > Mathf.Epsilon)
                {
                    float maxStep = turnSpeed * Owner.DeltaTime;
                    float step = Mathf.Min(maxStep, absAngle);
                    float deltaAngle = Mathf.Sign(snapshot.TurnAngle) * step;
                    modelRotator.RotateModelYaw(deltaAngle);
                }
            }
        }

        private static StringAsset ResolveMovingAlias(AnimancerStringProfile aliasProfile, EMovementGait gait)
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
