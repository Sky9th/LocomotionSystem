using UnityEngine;
using Animancer;
using RedDust.Gameplay.Character.Animation;
using RedDust.Gameplay.Character;

namespace RedDust.Gameplay.Character.Animation.Drivers.HitReaction
{
    /// <summary>
    /// 受击动画 CustomData 结构体。由 CharacterCombat 构建，HitReactionDriver.OnStarted 解包。
    /// </summary>
    internal struct SHitReactionData
    {
        public MixerTransition2D Mixer;
        public float DirX;
        public float DirY;
    }

    /// <summary>
    /// 受击动画驱动。CharacterCombat.OnReaction() 构建 AnimationRequest(DriverType=HitReaction)
    /// → brain.SubmitRequest(request) → Arbiter 仲裁 → OnStarted 播放受击混合动画。
    /// </summary>
    internal sealed class HitReactionDriver : BaseAnimationDriver
    {
        public override int ChannelMask => 1 << 0; // FullBody

        private AnimationRequest _currentRequest;

        public override void Evaluate(in SCharacterFrameContext ctx, float dt) { }
        public override void Drive(in SCharacterFrameContext ctx, float dt) { }

        public override void OnStarted(AnimationRequest request)
        {
            _currentRequest = request;
            var data = (SHitReactionData)request.CustomData;

            // Play mixer with request-specified fade.
            // AnimancerLayer.Play(ITransition) reads FadeDuration from the transition asset;
            // to respect request.FadeIn, temporarily override and restore after Play.
            var originalFade = data.Mixer.FadeDuration;
            data.Mixer.FadeDuration = request.FadeIn;
            var state = brain.FullBodyLayer.Play(data.Mixer);
            data.Mixer.FadeDuration = originalFade;

            if (state is MixerState<Vector2> mixerState)
                mixerState.Parameter = new Vector2(data.DirX, data.DirY);
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
