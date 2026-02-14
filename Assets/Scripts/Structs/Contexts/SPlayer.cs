using System;
using UnityEngine;

/// <summary>
/// Immutable snapshot describing the player character identity.
/// Wraps a generic SCharacter so other character types can share
/// the same base layout while extending with their own fields.
///
/// This struct is used as the payload when broadcasting that the
/// player has spawned into the world.
/// </summary>
[Serializable]
public struct SPlayer
{
    public SPlayer(SCharacter character, bool isLocalPlayer)
    {
        Character = character;
        IsLocalPlayer = isLocalPlayer;
    }

    /// <summary>Base character snapshot shared with other character types.</summary>
    public SCharacter Character { get; }

    /// <summary>Whether this snapshot represents the local controllable player.</summary>
    public bool IsLocalPlayer { get; }

    /// <summary>
    /// Convenience factory for constructing a player snapshot directly
    /// from a root Transform.
    /// </summary>
    public static SPlayer FromTransform(Transform root, bool isLocalPlayer)
    {
        if (root == null)
        {
            return new SPlayer(SCharacter.Default, isLocalPlayer);
        }

        var character = new SCharacter(root.gameObject.GetInstanceID(), root.position, root.rotation);
        return new SPlayer(character, isLocalPlayer);
    }

    public static SPlayer Default => new SPlayer(SCharacter.Default, true);
}
