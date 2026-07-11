using RedDust.Core.Events;
using UnityEngine;

namespace RedDust.Services.EntityService
{
    [CreateAssetMenu(menuName = "RedDust/Events/Entity/Spawned", fileName = "EntitySpawned")]
    public sealed class EntitySpawnedEvent : GameEvent<SEntitySpawned> { }
}
