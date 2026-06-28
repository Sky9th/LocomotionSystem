using RedDust.Properties;
using UnityEngine;

namespace RedDust.Entities
{
    /// <summary>
    /// 请求生成实体。由外部系统发布。
    /// Preset=null 时为已存在 Entity 生成 GO；Position=null 时只创建数据不进世界（进容器）。
    /// </summary>
    public readonly struct SEntitySpawnRequest
    {
        public readonly PropertyPresetSO Preset;
        public readonly string EntityId;
        public readonly Vector3? Position;
        public readonly Quaternion Rotation;

        public SEntitySpawnRequest(PropertyPresetSO preset, string entityId, Vector3? position, Quaternion rotation)
        {
            Preset = preset;
            EntityId = entityId;
            Position = position;
            Rotation = rotation;
        }

        public SEntitySpawnRequest(PropertyPresetSO preset, string entityId, Vector3? position)
            : this(preset, entityId, position, Quaternion.identity) { }

        public SEntitySpawnRequest(PropertyPresetSO preset)
            : this(preset, null, null, Quaternion.identity) { }
    }
}
