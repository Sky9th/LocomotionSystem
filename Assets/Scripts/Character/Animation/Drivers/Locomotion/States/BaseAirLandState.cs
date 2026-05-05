namespace Game.Character.Animation.Drivers
{
    internal sealed class BaseAirLandState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseAirLandState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
            => Owner.Snapshot.Kinematic.GroundContact.DistanceToGround < Owner.AnimProfile.landDistanceThreshold;

        public override bool CanExitState => true;

        public override void OnEnterState() => Owner.Play(Owner.Alias.AirLand);

        public override void Tick()
        {
            if (!Owner.HasCompleted()) return;
            if (Owner.TrySetState(BaseStateKey.Idle)) return;
            if (Owner.TrySetState(BaseStateKey.Moving)) return;
            if (Owner.TrySetState(BaseStateKey.TurnInPlace)) return;
        }
    }
}
