using UnityEngine;
using Animancer;
using RedDust.Character.Animation;
using RedDust.Character;

namespace RedDust.Character.Animation.Drivers.Locomotion
{
    public sealed class LocomotionDriver : BaseAnimationDriver
    {
        private BaseLayer baseLayer;

        public override int ChannelMask => 1 << 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            // BaseLayer 创建推迟到 OnAssemble——此时 ctx 尚未创建
        }

        public override void OnAssemble()
        {
            base.OnAssemble();
            // BaseLayer 创建推迟到 OnWire——AnimationBrain.OnAssemble 需先完成图层初始化
        }

        public override void OnWire()
        {
            base.OnWire();
            brain = GetComponentInChildren<AnimationBrain>();
            var buildCtx = brain?.BuildContext;
            baseLayer = new BaseLayer(
                brain?.FullBodyLayer,
                buildCtx?.DefaultLocomotionSet,  // TODO: GripTable.Resolve(tags) 替换
                buildCtx?.LocomotionAnimConfig,
                buildCtx);
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
