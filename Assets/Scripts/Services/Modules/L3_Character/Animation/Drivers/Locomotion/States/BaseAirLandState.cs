using Animancer;
using UnityEngine;

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

        public override void OnEnterState()
        {
            var fallDist = Owner.MaxFallDistance;
            var transition = fallDist <= Owner.AnimProfile.landLightMaxFallDistance
                ? Owner.AnimSet?.landLight : Owner.AnimSet?.landHard;
            if (transition == null || transition.Animations.Length == 0)
            {
                Debug.LogWarning($"[BaseAirLand] {Owner.AnimSet?.name}: land animation is empty, skipping to Idle.");
                Owner.Rig?.SetSuppressGroundLock(false);
                Owner.ForceSetState(BaseStateKey.Idle);
                return;
            }

            Owner.Play(transition);

            // Gait 驱动 LinearMixer blend 参数: 0=Idle, 1=Walk, 2=Run/Sprint
            var state = Owner.CurrentAnimState as LinearMixerState;
            if (state != null)
            {
                int gait = (int)Owner.Ctx.Discrete.Gait;
                state.Parameter = Mathf.Min((float)gait, 2f);
            }

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
