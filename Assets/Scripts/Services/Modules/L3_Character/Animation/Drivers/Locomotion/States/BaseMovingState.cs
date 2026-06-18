using Animancer;

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

            var gait = Owner.Ctx.Discrete.Gait;
            var alias = gait switch
            {
                EMovementGait.Walk   => Owner.Alias.walkMixer,
                EMovementGait.Run    => Owner.Alias.runMixer,
                EMovementGait.Sprint => Owner.Alias.sprint,
                _ => Owner.Alias.walkMixer
            };
            Owner.PlayIfChanged(alias);

            float desiredGaitSpeed = Owner.LocoProfile != null ? Owner.LocoProfile.GetSpeed(Owner.Ctx.Discrete.Posture, gait) : 0f;
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
