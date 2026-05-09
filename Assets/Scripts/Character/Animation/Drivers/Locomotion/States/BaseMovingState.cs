using Animancer;

namespace Game.Character.Animation.Drivers
{
    internal sealed class BaseMovingState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseMovingState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
            => Owner.Snapshot.Locomotion.Discrete.Phase == ELocomotionPhase.GroundedMoving
            && !Owner.Snapshot.Locomotion.Discrete.IsTurning;

        public override void Tick()
        {
            if (Owner.TrySetState(BaseStateKey.TurnInMoving)) return;
            if (Owner.TrySetState(BaseStateKey.Idle)) return;
            if (Owner.TrySetState(BaseStateKey.AirLoop)) return;

            var gait = Owner.Snapshot.Locomotion.Discrete.Gait;
            var alias = gait switch
            {
                EMovementGait.Walk   => Owner.Alias.walkMixer,
                EMovementGait.Run    => Owner.Alias.runMixer,
                EMovementGait.Sprint => Owner.Alias.sprint,
                _ => Owner.Alias.walkMixer
            };
            Owner.PlayIfChanged(alias);

            if (Owner.Layer.CurrentState is Vector2MixerState mixer && Owner.LocoProfile.moveSpeed > 0f)
            {
                var parameter = Owner.Snapshot.Locomotion.Motor.ActualLocalVelocity / Owner.LocoProfile.moveSpeed;
                if (parameter.sqrMagnitude > 1f) parameter.Normalize();
                mixer.Parameter = parameter;
            }

            Owner.ApplyTurnStepRotation();
        }
    }
}
