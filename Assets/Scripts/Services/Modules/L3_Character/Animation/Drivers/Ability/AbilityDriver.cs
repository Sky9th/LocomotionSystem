using RedDust.Ability;
using RedDust.Character;
using RedDust.Character.Animation;
using Animancer;

namespace RedDust.Character.Animation.Drivers.Ability
{
    /// <summary>
    /// 技能动画驱动。外部构建 AnimationRequest（DriverType=Ability）→ brain.SubmitRequest(request)
    /// → Arbiter 仲裁 → OnStarted 播放。Animancer 事件触发 OnMarker；OnCompleted/OnInterrupted 触发对应回调。
    /// </summary>
    internal sealed class AbilityDriver : BaseAnimationDriver
    {
        public override int ChannelMask => 1 << 0;

        private AnimationRequest _currentRequest;

        public override void Evaluate(in SCharacterFrameContext ctx, float dt) { }
        public override void Drive(in SCharacterFrameContext ctx, float dt) { }

        public override void OnStarted(AnimationRequest request)
        {
            _currentRequest = request;

            if (!request.HasClip || brain == null) return;

            var activation = request.CustomData as AbilityActivationSO;
            var state = brain.FullBodyLayer.Play(request.Clip, request.FadeIn);

            if (activation != null)
                state.Speed = activation.animationSpeed > 0f ? activation.animationSpeed : 1f;

            // 注入激发帧 Animancer 事件
            if (activation?.animationClip != null && activation.windupDuration > 0f)
            {
                float fireNorm = activation.windupDuration / activation.animationClip.length;
                if (fireNorm < 1f && state.Events(this, out var events))
                    events.Add(fireNorm, () => request.OnMarker?.Invoke());
                else
                    request.OnMarker?.Invoke();
            }
            else
            {
                request.OnMarker?.Invoke(); // windupDuration=0 → 瞬发
            }
        }

        public override void OnCompleted()
        {
            _currentRequest?.OnCompleted?.Invoke();
            _currentRequest = null;
        }

        public override void OnInterrupted(AnimationRequest by)
        {
            _currentRequest?.OnInterrupt?.Invoke();
            _currentRequest = null;
        }

        public override void OnResumed() { }
    }
}
