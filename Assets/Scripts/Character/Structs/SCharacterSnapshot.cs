using System;
using System.Collections.Generic;
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
        Stats = null;
    }

    public SCharacterInputActions Input { get; }
    public SCharacterKinematic Kinematic { get; }
    public SLocomotionState Locomotion { get; }
    public Dictionary<string, (float current, float max)> Stats { get; set; }

    public static SCharacterSnapshot Default => new(SCharacterInputActions.None, SCharacterKinematic.Default, SLocomotionState.Default);
}
