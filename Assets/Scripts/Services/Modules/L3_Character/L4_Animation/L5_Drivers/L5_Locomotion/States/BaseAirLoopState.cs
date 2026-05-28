namespace RedDust.Character.Animation.Drivers.Locomotion
{
    internal sealed class BaseAirLoopState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseAirLoopState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
        {
            get
            {
                var contact = Owner.Ctx.Kinematic.GroundContact;
                return !contact.IsGrounded
                    && contact.DistanceToGround >= Owner.AnimProfile.landMinFallDistance;
            }
        }

        public override void OnEnterState()
        {
            Owner.Play(Owner.Alias.AirLoop);
            Owner.AirborneStartY = Owner.Ctx.Kinematic.Position.y;
            Owner.MaxFallDistance = 0f;
            Owner.Rig?.SetSuppressGroundLock(true);
        }

        public override void Tick()
        {
            float fall = Owner.AirborneStartY - Owner.Ctx.Kinematic.Position.y;
            if (fall > Owner.MaxFallDistance) Owner.MaxFallDistance = fall;

            if (Owner.TrySetState(BaseStateKey.AirLand)) return;
        }
    }
}
