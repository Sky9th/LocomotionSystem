using Animancer;
using Animancer.TransitionLibraries;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Config;
using Game.Locomotion.Motor;

namespace Game.Locomotion.Animation.Core
{
    /// <summary>
    /// Read-only context passed into locomotion animation layers each frame.
    /// Wraps the current locomotion snapshot and shared animation dependencies.
    /// </summary>
    internal readonly struct LocomotionAnimationContext
    {
        public readonly SCharacterSnapshot Snapshot;
        public readonly float DeltaTime;
        public readonly NamedAnimancerComponent Animancer;
        public readonly LocomotionAliasProfile Alias;
        public readonly LocomotionAnimationProfile Profile;
        public readonly LocomotionProfile LocomotionProfile;
        public readonly LocomotionMotor Transformer;

        public LocomotionAnimationContext(
            SCharacterSnapshot snapshot,
            float deltaTime,
            NamedAnimancerComponent animancer,
            LocomotionAliasProfile alias,
            LocomotionAnimationProfile profile,
            LocomotionProfile locomotionProfile,
            LocomotionMotor transformer)
        {
            Snapshot = snapshot;
            DeltaTime = deltaTime;
            Animancer = animancer;
            Alias = alias;
            Profile = profile;
            LocomotionProfile = locomotionProfile;
            Transformer = transformer;
        }
    }
}
