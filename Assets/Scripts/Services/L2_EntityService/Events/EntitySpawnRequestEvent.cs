using RedDust.Core.Events;
using UnityEngine;

namespace RedDust.Services.EntityService
{
    [CreateAssetMenu(menuName = "RedDust/Events/Entity/Spawn Request", fileName = "EntitySpawnRequest")]
    public sealed class EntitySpawnRequestEvent : GameEvent<SEntitySpawnRequest> { }
}
