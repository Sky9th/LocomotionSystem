using UnityEngine;
using Animancer;

namespace Game.Character.Animation.Requests
{
    public class AnimationRequest
    {
        // ── 播放 ──
        public AnimationClip Clip;
        public StringAsset Alias;
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

        public bool HasClip => Clip != null;
        public bool HasAlias => Alias != null;
    }
}
