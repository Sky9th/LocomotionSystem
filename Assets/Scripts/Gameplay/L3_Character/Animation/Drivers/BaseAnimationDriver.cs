using RedDust.Core.Modules;
using UnityEngine;
using RedDust.Core.Events;
using RedDust.Gameplay.Character.Animation;
using RedDust.Gameplay.Character;

namespace RedDust.Gameplay.Character.Animation.Drivers
{
    internal abstract class BaseAnimationDriver : ModuleChildMono, ICharacterAnimationDriver
    {
        protected AnimationBrain brain;

        public abstract int ChannelMask { get; }
        public abstract void Evaluate(in SCharacterFrameContext ctx, float dt);
        public abstract void Drive(in SCharacterFrameContext ctx, float dt);
        public abstract void OnStarted(AnimationRequest request);
        public abstract void OnCompleted();
        public abstract void OnInterrupted(AnimationRequest by);
        public abstract void OnResumed();

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnDestroy()
        {
            brain?.UnregisterDriver(this);
        }

        public override void OnWire()
        {
            base.OnWire();
            brain = GetComponentInChildren<AnimationBrain>();
            brain?.RegisterDriver(this);
        }
    }
}
