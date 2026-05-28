using System;
using UnityEngine;

/// <summary>
/// Immutable snapshot describing a generic character instance in the world.
/// This base struct is intended to be reused by concrete character types
/// (e.g. player, zombies, NPCs) so they share a consistent identity layout.
/// </summary>
[Serializable]
public struct SCharacter
{
    public SCharacter(int instanceId, Vector3 position, Quaternion rotation)
    {
        InstanceId = instanceId;
        Position = position;
        Rotation = rotation;
    }

    /// <summary>Unity instance id of the root GameObject for this character.</summary>
    public int InstanceId { get; }

    /// <summary>World-space position of the character root.</summary>
    public Vector3 Position { get; }

    /// <summary>World-space rotation of the character root.</summary>
    public Quaternion Rotation { get; }

    public static SCharacter Default => new SCharacter(0, Vector3.zero, Quaternion.identity);
}
