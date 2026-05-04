using UnityEngine;
using Animancer;
using Game.Character.Animation.Requests;
using Game.Character.Locomotion;
using Game.Locomotion.Animation.Config;

namespace Game.Character.Animation.Drivers
{
    public sealed class LocomotionDriver : BaseCharacterAnimationDriver
    {
        [SerializeField] private LocomotionAliasProfile aliasProfile;
        [SerializeField] private LocomotionAnimationProfile animationProfile;
        [SerializeField] private LocomotionProfile locomotionProfile;

        private BaseLayer baseLayer;
        private AnimancerLayer headLookLayer;

        public override int ChannelMask => 1 << 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            baseLayer = new BaseLayer(brain?.FullBodyLayer, aliasProfile, locomotionProfile);
            headLookLayer = brain?.HeadLookLayer;

            if (headLookLayer != null && aliasProfile?.lookMixer != null)
            {
                var mixer = headLookLayer.TryPlay(aliasProfile.lookMixer) as Vector2MixerState;
                if (mixer != null) { mixer.Parameter = Vector2.zero; }
            }
        }

        public override void Drive(in SCharacterSnapshot snapshot, float dt)
        {
            baseLayer.Update(snapshot, dt);
        }

        public override void OnInterrupted(AnimationRequest by) { }
        public override void OnResumed() { }
    }
}
