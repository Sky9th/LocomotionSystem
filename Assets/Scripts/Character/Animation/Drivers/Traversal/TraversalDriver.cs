using UnityEngine;
using Animancer;
using Game.Character.Animation.Requests;

namespace Game.Character.Animation.Drivers
{
    public sealed class TraversalDriver : BaseCharacterAnimationDriver
    {
        [SerializeField] private AnimationClip climbClip;

        private bool wasJumpPressed;

        public override int ChannelMask => 1 << 0; // FullBody

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void Drive(in SCharacterSnapshot snapshot, float dt)
        {
            // OneShot: 只在 Active 时被调用, 不需要 Tick
        }

        public override void OnInterrupted(AnimationRequest by) { }
        public override void OnResumed() { }

        private void Update()
        {
            var inp = UnityEngine.InputSystem.Keyboard.current;
            if (inp == null) return;

            bool pressed = inp.spaceKey.wasPressedThisFrame;
            if (pressed && !wasJumpPressed && climbClip != null)
            {
                brain?.SubmitRequest(this, new AnimationRequest
                {
                    Clip = climbClip,
                    Tags = 0x01,
                    Resistance = 10,
                    OnComplete = OnCompleteBehavior.Resume,
                    OnInterrupted = OnInterruptedBehavior.Resume,
                    ChannelMask = 1 << 0
                });
            }
            wasJumpPressed = pressed;
        }
    }
}
