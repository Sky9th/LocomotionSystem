using RedDust.Core.Events;
using UnityEngine;

namespace RedDust.Services.EntityService
{
    [CreateAssetMenu(menuName = "RedDust/Events/Entity/Despawned", fileName = "EntityDespawned")]
    public sealed class EntityDespawnedEvent : GameEvent<SEntityDespawned> { }
}
