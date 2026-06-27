using RedDust.Core;
using UnityEngine;

namespace RedDust.Entities
{
    [CreateAssetMenu(menuName = "RedDust/Events/Entity/Spawned", fileName = "EntitySpawned")]
    public sealed class EntitySpawnedEvent : GameEvent<SEntitySpawned> { }
}
