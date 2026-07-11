using RedDust.Core.Events;
using UnityEngine;

namespace RedDust.Services.EntityService
{
    [CreateAssetMenu(menuName = "RedDust/Events/Entity/Despawn Request", fileName = "EntityDespawnRequest")]
    public sealed class EntityDespawnRequestEvent : GameEvent<SEntityDespawnRequest> { }
}
