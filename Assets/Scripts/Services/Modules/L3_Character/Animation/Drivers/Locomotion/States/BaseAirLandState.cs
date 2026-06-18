using Animancer;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    internal sealed class BaseAirLandState : LocomotionLayerFsmState<BaseLayer>
    {
        public BaseAirLandState(BaseLayer owner) : base(owner) { }

        public override bool CanEnterState
        {
            get
            {
                float fallDist = Owner.MaxFallDistance;
                var profile = Owner.AnimProfile;
                float threshold = fallDist <= profile.landLightMaxFallDistance
                    ? profile.landLightTriggerDistance
                    : fallDist <= profile.landMediumMaxFallDistance
                        ? profile.landMediumTriggerDistance
                        : profile.landHardTriggerDistance;

                return Owner.Ctx.Kinematic.GroundContact.DistanceToGround < threshold;
            }
        }

        public override bool CanExitState => true;

        // TODO: 寻路攀爬系统补齐后，每个 Mixer 按 Gait 参数混合: 0=Idle, 1=Walk, 2=Run/Sprint
        //       Gait 取自 BaseLayer.AirborneGait（AirLoop 进入时捕获），当前默认 0
        public override void OnEnterState()
        {
            float fallDist = Owner.MaxFallDistance;
            var profile = Owner.AnimProfile;

            StringAsset alias;
            if (fallDist <= profile.landLightMaxFallDistance)
                alias = Owner.Alias.LandLight;
            else
                alias = Owner.Alias.LandHard;

            Owner.Play(alias);
            Owner.Rig?.SetSuppressGroundLock(true);
        }

        public override void Tick()
        {
            if (!Owner.HasCompleted()) return;

            Owner.Rig?.SetSuppressGroundLock(false);

            if (Owner.TrySetState(BaseStateKey.Idle)) return;
            if (Owner.TrySetState(BaseStateKey.Moving)) return;
            if (Owner.TrySetState(BaseStateKey.TurnInPlace)) return;
            Owner.ForceSetState(BaseStateKey.Idle);
        }
    }
}
