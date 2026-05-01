using System;
using UnityEngine;

[Serializable]
public struct SCharacterSnapshot
{
    public SCharacterSnapshot(SCharacterKinematic kinematic, SLocomotionState locomotion)
    {
        Kinematic = kinematic;
        Locomotion = locomotion;
    }

    public SCharacterKinematic Kinematic { get; }
    public SLocomotionState Locomotion { get; }

    public static SCharacterSnapshot Default => new(SCharacterKinematic.Default, SLocomotionState.Default);
}
