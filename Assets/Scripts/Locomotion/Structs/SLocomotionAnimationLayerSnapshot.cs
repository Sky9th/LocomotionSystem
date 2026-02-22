using System;
using Animancer;

/// <summary>
/// Per-layer snapshot describing the animation currently playing on a
/// specific logical locomotion animation layer.
/// </summary>
[Serializable]
public struct SLocomotionAnimationLayerSnapshot
{
    public SLocomotionAnimationLayerSnapshot(
        string layerName,
        StringAsset alias,
        float normalizedTime,
        bool isTurnAnimation)
    {
        LayerName = layerName;
        Alias = alias;
        NormalizedTime = normalizedTime;
        IsTurnAnimation = isTurnAnimation;
    }

    /// <summary>Logical name of the locomotion animation layer that produced this snapshot.</summary>
    public string LayerName { get; }

    /// <summary>The primary animation alias on this layer, if any.</summary>
    public StringAsset Alias { get; }

    /// <summary>Current normalized time of the animation on this layer.</summary>
    public float NormalizedTime { get; }

    /// <summary>True if this layer is currently playing a turn animation.</summary>
    public bool IsTurnAnimation { get; }
}
