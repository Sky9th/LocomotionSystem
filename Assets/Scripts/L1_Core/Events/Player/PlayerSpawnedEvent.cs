using RedDust.Core;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Player/PlayerSpawned", fileName = "PlayerSpawnedEvent")]
    public sealed class PlayerSpawnedEvent : GameEvent<SPlayerSpawnedEvent> { }
}
