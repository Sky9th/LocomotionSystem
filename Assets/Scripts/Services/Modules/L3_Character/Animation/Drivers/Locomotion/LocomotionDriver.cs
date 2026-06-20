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

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnAssemble()
        {
            base.OnAssemble();
        }

        public override void OnWire()
        {
            base.OnWire();
            brain = GetComponentInChildren<AnimationBrain>();
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
            // TODO: 每帧 Resolve 仅为测试。事件驱动后 grip 变化时一次性计算，存到 buildCtx 缓存。
            var animSet = buildCtx?.GripTable?.Resolve(buildCtx?.Ability?.OwnedTags) ?? defaultAnimSet;

            // grip 未变化，跳过
            if (animSet == lastAnimSet) return;
            lastAnimSet = animSet;

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
