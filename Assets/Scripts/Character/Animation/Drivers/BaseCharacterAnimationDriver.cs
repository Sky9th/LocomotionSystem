using UnityEngine;
using Game.Character.Animation.Components;
using Game.Character.Animation.Requests;
using Game.Character.Components;

namespace Game.Character.Animation.Drivers
{
    public abstract class BaseCharacterAnimationDriver : MonoBehaviour, ICharacterAnimationDriver
    {
        protected AnimationBrain brain;

        public abstract int ChannelMask { get; }
        public abstract void Evaluate(in CharacterFrameContext ctx, float dt);
        public abstract void Drive(in CharacterFrameContext ctx, float dt);
        public abstract void OnStarted();
        public abstract void OnCompleted();
        public abstract void OnInterrupted(AnimationRequest by);
        public abstract void OnResumed();

        protected virtual void OnEnable()
        {
            brain = GetComponentInParent<AnimationBrain>();
            brain?.RegisterDriver(this);
        }

        protected virtual void OnDisable()
        {
            brain?.UnregisterDriver(this);
        }
    }
}
