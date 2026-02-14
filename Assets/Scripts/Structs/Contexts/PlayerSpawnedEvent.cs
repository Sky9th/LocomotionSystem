using UnityEngine;

/// <summary>
/// Event payload emitted when a player character has been spawned into the world.
/// Contains direct references so high-level systems (camera, UI, etc.) can
/// immediately bind to the correct transforms without additional lookups.
/// </summary>
public sealed class PlayerSpawnedEvent
{
    public PlayerSpawnedEvent(Transform root, bool isLocalPlayer)
    {
        Root = root;
        IsLocalPlayer = isLocalPlayer;
    }

    /// <summary>Root transform of the spawned player character.</summary>
    public Transform Root { get; }

    /// <summary>Whether this player instance is controlled by the local user.</summary>
    public bool IsLocalPlayer { get; }
}
