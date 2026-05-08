using System;
using UnityEngine;

[Serializable]
public struct SCharacterSnapshot
{
    public SCharacterSnapshot(
        SCharacterKinematic kinematic,
        SLocomotionMotor motor,
        SLocomotionDiscrete discreteState,
        SLocomotionTraversal traversal)
    {
        Kinematic     = kinematic;
        Motor         = motor;
        DiscreteState = discreteState;
        Traversal     = traversal;
    }

    public SCharacterKinematic Kinematic { get; }
    public SLocomotionMotor Motor { get; }
    public SLocomotionDiscrete DiscreteState { get; }
    public SLocomotionTraversal Traversal { get; }

    public static SCharacterSnapshot Default => new(
        SCharacterKinematic.Default,
        SLocomotionMotor.Default,
        SLocomotionDiscrete.Default,
        SLocomotionTraversal.None);
}
