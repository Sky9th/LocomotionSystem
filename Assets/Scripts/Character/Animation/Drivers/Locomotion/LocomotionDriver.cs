using UnityEngine;
using Animancer;
using Game.Character.Animation.Requests;

namespace Game.Character.Animation.Drivers
{
    public sealed class LocomotionDriver : BaseCharacterAnimationDriver
    {
        [SerializeField] private AnimationClip testClip;

        private AnimancerLayer layer;

        public override int ChannelMask => 1 << 0; // FullBody

        protected override void OnEnable()
        {
            base.OnEnable();
            layer = brain?.FullBodyLayer;
        }

        public override void Drive(in SCharacterSnapshot snapshot, float dt)
        {
            if (testClip != null && layer != null)
                layer.Play(testClip);
        }

        public override void OnInterrupted(AnimationRequest by) { }
        public override void OnResumed() { }
    }
}
