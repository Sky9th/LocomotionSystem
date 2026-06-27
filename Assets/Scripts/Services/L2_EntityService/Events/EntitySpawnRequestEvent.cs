using RedDust.Core;
using UnityEngine;

namespace RedDust.Entities
{
    [CreateAssetMenu(menuName = "RedDust/Events/Entity/Spawn Request", fileName = "EntitySpawnRequest")]
    public sealed class EntitySpawnRequestEvent : GameEvent<SEntitySpawnRequest> { }
}
