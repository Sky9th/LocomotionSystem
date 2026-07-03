using UnityEngine;

namespace RedDust.Core
{
    /// <summary>
    /// Event payload emitted when a player character has been spawned into the world.
    /// </summary>
    public readonly struct SPlayerSpawnedEvent
    {
        public SPlayerSpawnedEvent(Transform root, string entityId, bool isLocalPlayer)
        {
            Root = root;
            EntityId = entityId;
            IsLocalPlayer = isLocalPlayer;
        }

        /// <summary>Root transform of the spawned player character.</summary>
        public Transform Root { get; }

        /// <summary>Player Entity ID — 消费者通过 EntityService 自行 lookup。</summary>
        public string EntityId { get; }

        /// <summary>Whether this player instance is controlled by the local user.</summary>
        public bool IsLocalPlayer { get; }
    }
}
