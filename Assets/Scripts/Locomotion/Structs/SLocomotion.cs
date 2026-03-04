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
        SLocomotionAgent agent,
        SLocomotionDiscrete discreteState,
        SLocomotionAnimation animation = default)
    {
        Agent = agent;
        DiscreteState = discreteState;
        Animation = animation;
    }

    public SLocomotionAgent Agent { get; }
    public SLocomotionDiscrete DiscreteState { get; }

    /// <summary>
    /// Optional animation output produced by the locomotion animation module.
    /// Kept as a structured sub-snapshot to reduce clutter on the root DTO.
    /// </summary>
    public SLocomotionAnimation Animation { get; }

    public static SLocomotion Default => new SLocomotion(
        SLocomotionAgent.Default,
        new SLocomotionDiscrete(
            ELocomotionPhase.GroundedIdle,
            EPosture.Standing,
            EMovementGait.Idle,
            ELocomotionCondition.Normal,
            isTurning: false),
        SLocomotionAnimation.None);
}
