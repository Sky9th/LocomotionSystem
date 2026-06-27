using RedDust.Core;
using UnityEngine;

namespace RedDust.Entities
{
    [CreateAssetMenu(menuName = "RedDust/Events/Entity/Despawn Request", fileName = "Event_EntityDespawnRequest")]
    public sealed class EntityDespawnRequestEvent : GameEvent<SEntityDespawnRequest> { }
}
