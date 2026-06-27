using RedDust.Properties;
using UnityEngine;

namespace RedDust.Entities
{
    /// <summary>
    /// 请求生成实体 GO。由外部系统发布，EntityService 订阅后创建 Entity + Instantiate。
    /// 请求方不需要知道 Entity.Id——EntityService 分配。
    /// </summary>
    public readonly struct SEntitySpawnRequest
    {
        public readonly PropertyPresetSO Preset;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public SEntitySpawnRequest(PropertyPresetSO preset, Vector3 position, Quaternion rotation)
        {
            Preset = preset;
            Position = position;
            Rotation = rotation;
        }

        public SEntitySpawnRequest(PropertyPresetSO preset, Vector3 position)
            : this(preset, position, Quaternion.identity) { }

        public SEntitySpawnRequest(PropertyPresetSO preset)
            : this(preset, Vector3.zero, Quaternion.identity) { }
    }
}
