using Animancer;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.Discrete.Aspects;

namespace Game.Locomotion.Animation.Layers
{
    /// <summary>
    /// One-shot footstep layer.
    
    /// Uses the locomotion snapshot's leading-foot signal to trigger
    /// left/right footstep clips on a dedicated Animancer layer.
    ///
    /// This layer is intentionally minimal: it only concerns itself
    /// with deciding when to play a configured footstep alias.
    /// </summary>
    internal sealed class FootLayer : ILocomotionAnimationLayer
    {
        private const string FootstepLayerName = "Footstep";
        public int LayerIndex => 2;
        public AnimancerLayer Layer { get; set; }

        private StringAsset lastPlayedAlias;
        private AnimancerState currentState;
        private SLocomotionAnimationLayerSnapshot lastSnapshot;

        public string LayerName => FootstepLayerName;

        public SLocomotionAnimationLayerSnapshot AnimationSnapshot => lastSnapshot;

        public FootLayer(AnimancerLayer layer)
        {
            Layer = layer;
        }

        public void Update(in LocomotionAnimationContext context)
        {
            var animancer = context.Animancer;
            var alias = context.Alias;

            if (animancer == null || alias == null)
            {
                return;
            }

            //Layer.TryPlay(alias.runUp);
            //lastPlayedAlias = alias.sprint;

            UpdateSnapshot();
        }

        private void UpdateSnapshot()
        {
            float normalizedTime = currentState != null ? currentState.NormalizedTime : 0f;
            lastSnapshot = new SLocomotionAnimationLayerSnapshot(
                layerName: FootstepLayerName,
                alias: lastPlayedAlias,
                normalizedTime: normalizedTime);
        }
    }
}
