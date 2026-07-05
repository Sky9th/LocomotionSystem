using UnityEngine;

namespace RedDust.Character.Animation
{
    public enum EDriverType { Ability, Traversal, HitReaction }

    public class AnimationRequest
    {
        // ── 播放 ──
        public AnimationClip Clip;
        public float FadeIn;
        public float FadeOut;

        // ── 协商 ──
        public int Tags;
        public int Resistance;

        // ── 行为 ──
        public OnCompleteBehavior OnComplete;
        public OnInterruptedBehavior OnInterrupted;

        // ── 占哪层 ──
        public int ChannelMask;

        /// <summary>调用方可选附加数据（如 AbilityActivationSO）。Driver 内部 cast。</summary>
        public object CustomData;

        /// <summary>Driver 内部标记事件触发时调用（如激发帧到达）。</summary>
        public System.Action<AnimationRequest> OnMarker;

        /// <summary>动画正常播完时调用。</summary>
        public System.Action<AnimationRequest> OnCompleted;

        /// <summary>动画被中断时调用。</summary>
        public System.Action<AnimationRequest> OnInterrupt;

        /// <summary>Brain 据此路由到对应的 Driver。</summary>
        public EDriverType DriverType;

        public bool HasClip => Clip != null;
    }
}
