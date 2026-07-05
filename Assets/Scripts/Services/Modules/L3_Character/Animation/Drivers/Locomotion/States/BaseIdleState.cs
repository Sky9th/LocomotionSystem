using Animancer;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    internal sealed class BaseIdleState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseIdleState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
            => Owner.Ctx.Discrete.Phase == ELocomotionPhase.GroundedIdle
            && !Owner.Ctx.Discrete.IsTurning;

        private ITransition ResolveIdle()
        {
            if (Owner.IdleOverride != null) return Owner.IdleOverride;
            var animSet = Owner.AnimSet;
            if (Owner.Ctx.Discrete.Posture == EPosture.Crouching && animSet?.crouchIdle?.Clip != null)
                return animSet.crouchIdle;
            return animSet?.idleL;
        }

        public override void OnEnterState()
        {
            Owner.Play(ResolveIdle());
        }

        public override void Tick()
        {
            if (Owner.TrySetState(BaseStateKey.TurnInPlace)) return;
            if (Owner.TrySetState(BaseStateKey.Moving)) return;
            if (Owner.TrySetState(BaseStateKey.AirLoop)) return;
            Owner.PlayIfChanged(ResolveIdle());
        }
    }
}
