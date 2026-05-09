using System;
using Game.Character.Input;
using UnityEngine;

[Serializable]
public struct SCharacterSnapshot
{
    public SCharacterSnapshot(SCharacterInputActions input, SCharacterKinematic kinematic, SLocomotionState locomotion)
    {
        Input = input;
        Kinematic = kinematic;
        Locomotion = locomotion;
    }

    public SCharacterInputActions Input { get; }
    public SCharacterKinematic Kinematic { get; }
    public SLocomotionState Locomotion { get; }

    public static SCharacterSnapshot Default => new(SCharacterInputActions.None, SCharacterKinematic.Default, SLocomotionState.Default);
}
