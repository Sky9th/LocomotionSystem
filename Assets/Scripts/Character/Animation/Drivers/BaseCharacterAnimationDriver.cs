using UnityEngine;
using Game.Character.Animation.Components;
using Game.Character.Animation.Requests;

namespace Game.Character.Animation.Drivers
{
    public abstract class BaseCharacterAnimationDriver : MonoBehaviour, ICharacterAnimationDriver
    {
        protected AnimationBrain brain;

        public abstract int ChannelMask { get; }
        public abstract void Drive(in SCharacterSnapshot snapshot, float dt);
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
