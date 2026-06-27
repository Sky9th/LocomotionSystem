using RedDust.Core;
using UnityEngine;

namespace RedDust.Entities
{
    [CreateAssetMenu(menuName = "RedDust/Events/Entity/Despawned", fileName = "EntityDespawned")]
    public sealed class EntityDespawnedEvent : GameEvent<SEntityDespawned> { }
}
