using RedDust.Core;
using UnityEngine;

namespace RedDust.Entities
{
    [CreateAssetMenu(menuName = "RedDust/Events/Entity/Despawn Request", fileName = "EntityDespawnRequest")]
    public sealed class EntityDespawnRequestEvent : GameEvent<SEntityDespawnRequest> { }
}
