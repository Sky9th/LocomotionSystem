using System;
using System.Collections.Generic;
using Animancer;
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
        private readonly Dictionary<string, SLocomotionAnimationLayerSnapshot> layerSnapshots;

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
            layerSnapshots = new Dictionary<string, SLocomotionAnimationLayerSnapshot>(this.layers.Length);
        }

        public IReadOnlyDictionary<string, SLocomotionAnimationLayerSnapshot> AnimationSnapshots => layerSnapshots;

        public void UpdateAnimations(SLocomotion snapshot, float deltaTime)
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

            layerSnapshots.Clear();
            int layerCount = layers.Length;
            for (int i = 0; i < layerCount; i++)
            {
                var layer = layers[i];
                if (layer == null)
                {
                    continue;
                }

                layer.Update(in context);

                var layerSnapshot = layer.AnimationSnapshot;
                if (!string.IsNullOrEmpty(layerSnapshot.LayerName))
                {
                    layerSnapshots[layerSnapshot.LayerName] = layerSnapshot;
                }
            }
        }
    }
}
