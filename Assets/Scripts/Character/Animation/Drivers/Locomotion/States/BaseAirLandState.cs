using Animancer;

namespace Game.Character.Animation.Drivers
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

                return Owner.Snapshot.Kinematic.GroundContact.DistanceToGround < threshold;
            }
        }

        public override bool CanExitState => true;

        public override void OnEnterState()
        {
            float fallDist = Owner.MaxFallDistance;
            var profile = Owner.AnimProfile;

            StringAsset alias;
            if (fallDist <= profile.landLightMaxFallDistance)
                alias = Owner.Alias.LandLight;
            else if (fallDist <= profile.landMediumMaxFallDistance)
                alias = Owner.Alias.LandMedium;
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
            if (Owner.TrySetState(BaseStateKey.IdleToMoving)) return;
            if (Owner.TrySetState(BaseStateKey.TurnInPlace)) return;
            Owner.ForceSetState(BaseStateKey.Idle);
        }
    }
}
