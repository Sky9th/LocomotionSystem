using System;
using UnityEngine;
using Game.Locomotion.Discrete.Structs;

/// <summary>
/// Immutable snapshot describing the player's locomotion state at a given frame.
/// </summary>
[Serializable]
public struct SLocomotion
{
    public SLocomotion(
        SLocomotionMotor motor,
        SLocomotionDiscrete discreteState,
        SLocomotionTraversal traversal,
        SLocomotionAnimation animation = default)
    {
        Motor = motor;
        DiscreteState = discreteState;
        Traversal = traversal;
        Animation = animation;
    }

    public SLocomotion(
        SLocomotionMotor motor,
        SLocomotionDiscrete discreteState,
        SLocomotionAnimation animation = default)
        : this(motor, discreteState, SLocomotionTraversal.None, animation)
    {
    }

    public SLocomotionMotor Motor { get; }
    public SLocomotionDiscrete DiscreteState { get; }
    public SLocomotionTraversal Traversal { get; }

    /// <summary>
    /// Optional animation output produced by the locomotion animation module.
    /// Kept as a structured sub-snapshot to reduce clutter on the root DTO.
    /// </summary>
    public SLocomotionAnimation Animation { get; }

    public static SLocomotion Default => new SLocomotion(
        SLocomotionMotor.Default,
        SLocomotionDiscrete.Default,
        SLocomotionTraversal.None,
        SLocomotionAnimation.None);
}
