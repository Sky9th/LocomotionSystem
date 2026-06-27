using UnityEngine;

namespace RedDust.Entities
{
    /// <summary>
    /// 请求生成实体 GO。由外部系统（PlayerService、场景管理器）发布，
    /// EntityService 订阅并执行 Spawn。
    /// </summary>
    public readonly struct SEntitySpawnRequest
    {
        public readonly string EntityId;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public SEntitySpawnRequest(string entityId, Vector3 position, Quaternion rotation)
        {
            EntityId = entityId;
            Position = position;
            Rotation = rotation;
        }

        public SEntitySpawnRequest(string entityId, Vector3 position)
            : this(entityId, position, Quaternion.identity) { }

        public SEntitySpawnRequest(string entityId)
            : this(entityId, Vector3.zero, Quaternion.identity) { }
    }
}
