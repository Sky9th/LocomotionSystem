using Animancer;
using Animancer.TransitionLibraries;
using Game.Locomotion.Animation.Config;

namespace Game.Locomotion.Animation.Core
{
    /// <summary>
    /// Read-only context passed into locomotion animation layers each frame.
    /// Wraps the current locomotion snapshot and shared animation dependencies.
    /// </summary>
    internal readonly struct LocomotionAnimationContext
    {
        public readonly SPlayerLocomotion Snapshot;
        public readonly float DeltaTime;
        public readonly NamedAnimancerComponent Animancer;
        public readonly AnimancerStringProfile Alias;
        public readonly LocomotionAnimationProfile Profile;

        public LocomotionAnimationContext(
            SPlayerLocomotion snapshot,
            float deltaTime,
            NamedAnimancerComponent animancer,
            AnimancerStringProfile alias,
            LocomotionAnimationProfile profile)
        {
            Snapshot = snapshot;
            DeltaTime = deltaTime;
            Animancer = animancer;
            Alias = alias;
            Profile = profile;
        }
    }
}
