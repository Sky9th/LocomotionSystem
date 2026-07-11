using RedDust.Gameplay.Ability;
using RedDust.Gameplay.Character;
using RedDust.Gameplay.Character.Animation;
using Animancer;
using UnityEngine;

namespace RedDust.Gameplay.Character.Animation.Drivers.Ability
{
    /// <summary>
    /// 技能动画驱动。外部构建 AnimationRequest（DriverType=Ability）→ brain.SubmitRequest(request)
    /// → Arbiter 仲裁 → OnStarted 播放。Animancer 事件触发 OnMarker；OnCompleted/OnInterrupted 触发对应回调。
    /// </summary>
    internal sealed class AbilityDriver : BaseAnimationDriver
    {
        public override int ChannelMask => 1 << 0;

        private AnimationRequest _currentRequest;
        private AnimancerEvent.Sequence _fireSequence;

        public override void Evaluate(in SCharacterFrameContext ctx, float dt) { }
        public override void Drive(in SCharacterFrameContext ctx, float dt) { }

        public override void OnStarted(AnimationRequest request)
        {
            _currentRequest = request;

            if (!request.HasClip || brain == null) return;

            var activation = request.CustomData as AbilityActivationSO;
            var state = brain.FullBodyLayer.Play(request.Clip, request.FadeIn);
            state.Time = 0f;  // 重复播放同一 clip 时从头开始

            if (activation != null)
                state.Speed = activation.animationSpeed > 0f ? activation.animationSpeed : 1f;

            // 注入激发帧 Animancer 事件
            if (activation?.animationClip != null && activation.windupDuration > 0f)
            {
                float fireNorm = activation.windupDuration / activation.animationClip.length;
                if (fireNorm < 1f)
                {
                    _fireSequence = null;
                    state.Events(ref _fireSequence);
                    _fireSequence.Clear();  // 清除旧 clip 复用残留的事件
                    _fireSequence.Add(fireNorm, () => request.OnMarker?.Invoke(request));
                }
            }
        }

        public override void OnCompleted()
        {
            _currentRequest?.OnCompleted?.Invoke(_currentRequest);
            _currentRequest = null;
        }

        public override void OnInterrupted(AnimationRequest by)
        {
            _currentRequest?.OnInterrupt?.Invoke(_currentRequest);
            _currentRequest = null;
        }

        public override void OnResumed() { }
    }
}
