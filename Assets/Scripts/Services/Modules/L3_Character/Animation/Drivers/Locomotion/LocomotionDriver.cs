using UnityEngine;
using Animancer;
using RedDust.Character.Animation;
using RedDust.Character;
using RedDust.Character.Locomotion;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    public sealed class LocomotionDriver : BaseCharacterAnimationDriver
    {
        private BaseLayer baseLayer;

        public override int ChannelMask => 1 << 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            var actor = GetComponent<CharacterActor>();
            baseLayer = new BaseLayer(
                brain?.FullBodyLayer,
                actor?.AnimationAliasProfile,
                actor?.LocomotionAnimationProfile,
                actor?.LocomotionProfile,
                brain?.CharacterRig);
        }

        public override void Evaluate(in CharacterFrameContext ctx, float dt) { }
        public override void OnStarted() { }
        public override void OnCompleted() { }

        public override void Drive(in CharacterFrameContext ctx, float dt)
        {
            baseLayer.Update(ctx, dt);
        }

        public override void OnInterrupted(AnimationRequest by) { }
        public override void OnResumed()
        {
            baseLayer.InvalidateAnimationCache();
        }

        internal BaseLayer BaseLayer => baseLayer;
    }
}
