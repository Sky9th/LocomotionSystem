using Animancer;

namespace Game.Character.Animation.Drivers
{
    internal sealed class BaseAirLoopState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseAirLoopState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
            => !Owner.Snapshot.Kinematic.GroundContact.IsGrounded;

        public override void OnEnterState() => Owner.Play(Owner.Alias.AirLoop);

        public override void Tick()
        {
            if (Owner.TrySetState(BaseStateKey.AirLand)) return;
        }
    }
}
