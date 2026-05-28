namespace RedDust.Character.Animation.Drivers.Locomotion
{
    internal sealed class BaseIdleToMovingState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseIdleToMovingState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
            => Owner.Ctx.Discrete.Phase == ELocomotionPhase.GroundedMoving
            && Owner.Ctx.Discrete.IsTurning;

        public override bool CanExitState
            => !Owner.Ctx.Discrete.IsTurning;

        public override void OnEnterState()
        {
            var alias = Owner.Ctx.Motor.TurnAngle > 0f
                ? Owner.Alias.idleToRun180R : Owner.Alias.idleToRun180L;
            Owner.PlayFromStart(alias);
        }

        public override void Tick()
        {
            if (Owner.TrySetState(BaseStateKey.Idle)) return;
            if (Owner.TrySetState(BaseStateKey.Moving)) return;
            if (Owner.TrySetState(BaseStateKey.AirLoop)) return;
            if (Owner.HasCompleted())
            {
                if (Owner.Ctx.Discrete.Phase == ELocomotionPhase.GroundedMoving)
                    Owner.ForceSetState(BaseStateKey.Moving);
                else
                    Owner.ForceSetState(BaseStateKey.Idle);
            }
        }
    }
}
