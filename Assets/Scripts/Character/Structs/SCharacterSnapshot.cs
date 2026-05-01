using System;
using UnityEngine;

[Serializable]
public struct SCharacterSnapshot
{
    public SCharacterSnapshot(SCharacterKinematic kinematic)
    {
        Kinematic = kinematic;
    }

    public SCharacterKinematic Kinematic { get; }

    public static SCharacterSnapshot Default => new(SCharacterKinematic.Default);
}
