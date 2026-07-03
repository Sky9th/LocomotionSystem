using RedDust.Character.Animation;
using RedDust.Character;

namespace RedDust.Character.Animation.Drivers.Ability
{
    /// <summary>
    /// 一次性动画驱动。被动响应 Arbiter 调度——被激活时播放当前 Request 的动画。
    ///
    /// 使用：brain.SubmitRequest(abilityDriver, request) → Arbiter 仲裁 → OnStarted 自播 → 播完自动归还。
    /// </summary>
    public sealed class AbilityDriver : BaseAnimationDriver
    {
        public override int ChannelMask => 1 << 0; // FullBody

        public override void Evaluate(in CharacterFrameContext ctx, float dt) { }
        public override void Drive(in CharacterFrameContext ctx, float dt) { }

        public override void OnStarted(AnimationRequest request)
        {
            var layer = brain.FullBodyLayer;
            if (request.HasClip)
                layer.Play(request.Clip, request.FadeIn);
        }

        public override void OnCompleted() { }
        public override void OnInterrupted(AnimationRequest by) { }
        public override void OnResumed() { }
    }
}
