using System;
using Animancer;
using Animancer.TransitionLibraries;
using Game.Locomotion.Animation.Config;

namespace Game.Locomotion.Animation.Core
{
    /// <summary>
    /// Central coordinator for locomotion animation.
    /// Consumes locomotion snapshots and forwards them to a set
    /// of orthogonal animation layers.
    /// </summary>
    internal sealed class LocomotionAnimationController
    {
        private readonly NamedAnimancerComponent animancer;
        private readonly AnimancerStringProfile alias;
        private readonly LocomotionAnimationProfile profile;
        private readonly ILocomotionAnimationLayer[] layers;

        public LocomotionAnimationController(
            NamedAnimancerComponent animancer,
            AnimancerStringProfile alias,
            LocomotionAnimationProfile profile,
            params ILocomotionAnimationLayer[] layers)
        {
            this.animancer = animancer;
            this.alias = alias;
            this.profile = profile;
            this.layers = layers ?? Array.Empty<ILocomotionAnimationLayer>();
        }

        public void UpdateAnimations(SPlayerLocomotion snapshot, float deltaTime)
        {
            if (animancer == null || alias == null || profile == null)
            {
                return;
            }

            var context = new LocomotionAnimationContext(
                snapshot,
                deltaTime,
                animancer,
                alias,
                profile);

            if (layers == null)
            {
                return;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                layers[i]?.Update(in context);
            }
        }
    }
}
