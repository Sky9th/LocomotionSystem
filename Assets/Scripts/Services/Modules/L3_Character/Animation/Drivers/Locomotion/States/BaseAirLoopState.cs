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

        // TODO: 寻路攀爬系统补齐后，按 fall 落差选择 AirLight / AirHard
        //       每个 Mixer 按 Gait 参数混合: 0=Idle, 1=Walk, 2=Run/Sprint（当前默认 0）
        // TODO: migrated to ITransition
        public override void OnEnterState()
        {
            // Owner.Play(Owner.Alias.airLight);
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
