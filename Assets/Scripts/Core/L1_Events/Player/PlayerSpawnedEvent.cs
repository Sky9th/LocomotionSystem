using RedDust.Core.Structs;
using RedDust.Core.Events;
using UnityEngine;

namespace RedDust.Core.Events
{
    [CreateAssetMenu(menuName = "RedDust/Events/Player/PlayerSpawned", fileName = "PlayerSpawnedEvent")]
    public sealed class PlayerSpawnedEvent : GameEvent<SPlayerSpawnedEvent> { }
}
