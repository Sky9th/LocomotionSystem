using UnityEngine;
using Animancer;
using Game.Character.Animation.Requests;
using Game.Character.Locomotion;
using Game.Locomotion.Animation.Config;

namespace Game.Character.Animation.Drivers
{
    public sealed class LocomotionDriver : BaseCharacterAnimationDriver
    {
        [SerializeField] private AnimationAliasProfile aliasProfile;
        [SerializeField] private LocomotionAnimationProfile animationProfile;
        [SerializeField] private LocomotionProfile locomotionProfile;

        private BaseLayer baseLayer;

        public override int ChannelMask => 1 << 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            baseLayer = new BaseLayer(brain?.FullBodyLayer, aliasProfile, animationProfile, locomotionProfile, brain?.CharacterRig);
        }

        public override void Evaluate(in SCharacterSnapshot snapshot, float dt) { }
        public override void OnStarted() { }
        public override void OnCompleted() { }

        public override void Drive(in SCharacterSnapshot snapshot, float dt)
        {
            baseLayer.Update(snapshot, dt);
        }

        public override void OnInterrupted(AnimationRequest by) { }
        public override void OnResumed()
        {
            baseLayer.InvalidateAnimationCache();
        }
    }
}
