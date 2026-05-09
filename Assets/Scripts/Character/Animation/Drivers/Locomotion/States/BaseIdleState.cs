using Animancer;

namespace Game.Character.Animation.Drivers
{
    internal sealed class BaseIdleState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseIdleState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
            => Owner.Snapshot.Locomotion.Discrete.Phase == ELocomotionPhase.GroundedIdle
            && !Owner.Snapshot.Locomotion.Discrete.IsTurning;

        public override void OnEnterState() => Owner.Play(Owner.Alias.idleL);

        public override void Tick()
        {
            if (Owner.TrySetState(BaseStateKey.TurnInPlace)) return;
            if (Owner.TrySetState(BaseStateKey.IdleToMoving)) return;
            if (Owner.TrySetState(BaseStateKey.Moving)) return;
            if (Owner.TrySetState(BaseStateKey.AirLoop)) return;
            Owner.PlayIfChanged(Owner.Alias.idleL);
        }
    }
}
