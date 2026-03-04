using System;

/// <summary>
/// Aggregated animation output produced by the locomotion animation module.
/// This data is intended to be attached to the main <see cref="SLocomotion"/>
/// snapshot so that external systems can query both simulation and animation
/// state from a single DTO.
/// </summary>
[Serializable]
public struct SLocomotionAnimation
{
    public SLocomotionAnimation(
        SLocomotionAnimationLayerSnapshot baseLayer,
        SLocomotionAnimationLayerSnapshot headLookLayer,
        SLocomotionAnimationLayerSnapshot footstepLayer)
    {
        BaseLayer = baseLayer;
        HeadLookLayer = headLookLayer;
        FootstepLayer = footstepLayer;
    }

    public SLocomotionAnimationLayerSnapshot BaseLayer { get; }
    public SLocomotionAnimationLayerSnapshot HeadLookLayer { get; }
    public SLocomotionAnimationLayerSnapshot FootstepLayer { get; }

    public bool HasAny =>
        !string.IsNullOrEmpty(BaseLayer.LayerName) ||
        !string.IsNullOrEmpty(HeadLookLayer.LayerName) ||
        !string.IsNullOrEmpty(FootstepLayer.LayerName);

    public static SLocomotionAnimation None => default;
}
