using Animancer;
using RedDust.Character;

namespace RedDust.Character.Animation.Drivers.Locomotion
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
                // TODO: Crawl mixer — 需在 LocomotionAnimationSetSO 中添加 crawlMixer 字段
                _ => null
            };
            Owner.PlayIfChanged(transition ?? Owner.AnimSet?.walkMixer);

            // TODO: posture-aware speed — 当前仅按 gait 查 animNativeSpeed，姿势系数由 Properties 叠加
            float desiredGaitSpeed = Owner.AnimSet != null ? Owner.AnimSet.GetNativeSpeed(Owner.Ctx.Discrete.Gait) : 0f;
            if (Owner.Layer.CurrentState is Vector2MixerState mixer && desiredGaitSpeed > 0f)
            {
                var parameter = Owner.Ctx.Motor.ActualLocalVelocity / desiredGaitSpeed;
                if (parameter.sqrMagnitude > 1f) parameter.Normalize();
                mixer.Parameter = parameter;
            }

            Owner.ApplyTurnStepRotation();
        }
    }
}
