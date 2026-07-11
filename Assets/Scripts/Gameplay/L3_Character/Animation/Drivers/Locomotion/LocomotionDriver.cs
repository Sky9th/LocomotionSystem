using UnityEngine;
using Animancer;
using RedDust.Gameplay.Character.Animation;
using RedDust.Gameplay.Character;

namespace RedDust.Gameplay.Character.Animation.Drivers.Locomotion
{
    internal sealed class LocomotionDriver : BaseAnimationDriver
    {
        private BaseLayer baseLayer;
        private ArmPoseLayer armPoseLayer;

        public override int ChannelMask => 1 << 0;

        public override void OnWire()
        {
            base.OnWire();
            var buildCtx = brain?.BuildContext;
            baseLayer = new BaseLayer(
                brain?.FullBodyLayer,
                buildCtx?.DefaultLocomotionSet,
                buildCtx?.LocomotionAnimConfig,
                buildCtx);
            armPoseLayer = new ArmPoseLayer(brain?.ArmLayer, buildCtx);
        }

        public override void Evaluate(in SCharacterFrameContext ctx, float dt) { }
        public override void OnStarted(AnimationRequest request) { }
        public override void OnCompleted() { }

        public override void Drive(in SCharacterFrameContext ctx, float dt)
        {
            baseLayer.Update(ctx, dt);
            armPoseLayer.Update(ctx);
        }

        public override void OnInterrupted(AnimationRequest by)
        {
            armPoseLayer.FadeOut();
        }

        public override void OnResumed()
        {
            baseLayer.InvalidateAnimationCache();
            armPoseLayer.Invalidate();
        }

        internal BaseLayer BaseLayer => baseLayer;
    }
}
