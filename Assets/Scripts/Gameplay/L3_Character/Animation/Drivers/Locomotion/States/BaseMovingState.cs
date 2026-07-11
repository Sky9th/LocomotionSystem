using Animancer;
using RedDust.Gameplay.Character;

namespace RedDust.Gameplay.Character.Animation.Drivers.Locomotion
{
    internal sealed class BaseMovingState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseMovingState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
            => Owner.Ctx.Discrete.Phase == ELocomotionPhase.GroundedMoving;

        public override void Tick()
        {
            if (Owner.TrySetState(BaseStateKey.Idle)) return;
            if (Owner.TrySetState(BaseStateKey.AirLoop)) return;

            ITransition transition = Owner.Ctx.Discrete.Gait switch
            {
                EMovementGait.Walk => Owner.AnimSet?.walkMixer,
                EMovementGait.Run  => Owner.AnimSet?.runMixer,
                EMovementGait.Sprint => Owner.AnimSet?.sprint,
                EMovementGait.Crawl => Owner.AnimSet?.crouchMixer,
                _ => null
            };
            // 蹲姿减速到 Idle 时 phase 仍为 GroundedMoving，fallback 应用 crouchMixer 而非 walkMixer
            var fallback = Owner.Ctx.Discrete.Posture == EPosture.Crouching
                ? Owner.AnimSet?.crouchMixer : Owner.AnimSet?.walkMixer;
            Owner.PlayIfChanged(transition ?? fallback);

            float nativeGaitSpeed = Owner.AnimSet?.GetNativeSpeed(Owner.Ctx.Discrete.Gait) ?? 0f;
            float motionScale = Owner.Ctx.Discrete.MotionSpeedScale;
            float scaledGaitSpeed = nativeGaitSpeed * motionScale;
            if (Owner.Layer.CurrentState is Vector2MixerState mixer && scaledGaitSpeed > 0f)
            {
                var parameter = Owner.Ctx.Motor.ActualLocalVelocity / scaledGaitSpeed;
                if (parameter.sqrMagnitude > 1f) parameter.Normalize();
                mixer.Parameter = parameter;
            }

            Owner.ApplyTurnStepRotation();
        }
    }
}
