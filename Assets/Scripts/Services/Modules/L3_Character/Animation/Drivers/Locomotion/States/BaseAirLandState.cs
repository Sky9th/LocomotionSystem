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

        // TODO: 按 Gait 参数混合 landLight/landHard LinearMixer（0=Idle, 1=Walk, 2=Run/Sprint）
        public override void OnEnterState()
        {
            var fallDist = Owner.MaxFallDistance;
            var transition = fallDist <= Owner.AnimProfile.landLightMaxFallDistance
                ? Owner.AnimSet?.landLight : Owner.AnimSet?.landHard;
            Owner.Play(transition);
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
