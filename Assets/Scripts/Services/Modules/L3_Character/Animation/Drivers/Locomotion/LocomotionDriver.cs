using UnityEngine;
using Animancer;
using RedDust.Character.Animation;
using RedDust.Character;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    public sealed class LocomotionDriver : BaseAnimationDriver
    {
        private BaseLayer baseLayer;
        private LocomotionAnimationSetSO defaultAnimSet;
        private LocomotionAnimationSetSO lastAnimSet;

        public override int ChannelMask => 1 << 0;

        public override void OnWire()
        {
            base.OnWire();
            var buildCtx = brain?.BuildContext;
            defaultAnimSet = buildCtx?.DefaultLocomotionSet;
            baseLayer = new BaseLayer(
                brain?.FullBodyLayer,
                defaultAnimSet,
                buildCtx?.LocomotionAnimConfig,
                buildCtx);
        }

        public override void Evaluate(in CharacterFrameContext ctx, float dt)
        {
            var buildCtx = brain?.BuildContext;
            // 动画集由 CharacterActor 统一解析存入 BuildContext，此处只读
            var animSet = buildCtx?.ResolvedLocoAnimSet ?? defaultAnimSet;

            // 未变化，跳过
            if (animSet == lastAnimSet) return;
            lastAnimSet = animSet;

            var bodyForm = buildCtx?.BodyForm ?? EBodyForm.Relax;

            if (animSet.HasFullLocomotion)
            {
                // Full grip: swap BaseLayer，淡出 Arm 层
                baseLayer.AnimSet = animSet;
                brain?.ArmLayer?.StartFade(0, 0.25f);
            }
            else
            {
                // Partial grip: BaseLayer 保持 Unarmed，Arm 层叠武器 idle
                baseLayer.AnimSet = defaultAnimSet;
                var idle = animSet?.idleL;
                if (idle != null)
                    brain?.ArmLayer?.Play(idle);
                else
                    brain?.ArmLayer?.StartFade(0, 0.25f);
            }
        }

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
