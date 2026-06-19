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
                _ => null
            };
            Owner.PlayIfChanged(transition ?? Owner.AnimSet?.walkMixer);

            float desiredGaitSpeed = Owner.LocoProfile != null ? Owner.LocoProfile.GetSpeed(Owner.Ctx.Discrete.Posture, Owner.Ctx.Discrete.Gait) : 0f;
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
