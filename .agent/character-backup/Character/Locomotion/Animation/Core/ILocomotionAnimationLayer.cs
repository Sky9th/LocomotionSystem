using Animancer;

namespace Game.Locomotion.Animation.Core
{
    /// <summary>
    /// Base interface for all locomotion animation layers.
    /// Each layer is responsible for a single concern such as
    /// base movement, turning in place, airborne, upper body, etc.
    /// </summary>
    internal interface ILocomotionAnimationLayer
    {

        AnimancerLayer Layer { get; set; }
        int LayerIndex { get; }
        /// <summary>Logical name of this locomotion animation layer.</summary>
        string LayerName { get; }

        /// <summary>Update this animation layer for the current frame.</summary>
        void Update(in LocomotionAnimationContext context);

        /// <summary>Latest animation snapshot produced by this layer.</summary>
        SLocomotionAnimationLayerSnapshot AnimationSnapshot { get; }
    }
}
