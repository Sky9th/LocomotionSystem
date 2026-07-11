using RedDust.Gameplay.Properties;
using UnityEngine;

namespace RedDust.Services.EntityService
{
    /// <summary>
    /// 实体 GO 生成完成通知。EntityService 在 Spawn 成功后发布。
    /// 订阅方通过 EntityId 或 Preset 判断是否为自己请求的实体。
    /// </summary>
    public readonly struct SEntitySpawned
    {
        public readonly string EntityId;
        public readonly PropertyPresetSO Preset;
        public readonly GameObject View;

        public SEntitySpawned(string entityId, PropertyPresetSO preset, GameObject view)
        {
            EntityId = entityId;
            Preset = preset;
            View = view;
        }
    }
}
