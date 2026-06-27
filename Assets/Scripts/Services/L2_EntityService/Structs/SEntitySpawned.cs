using UnityEngine;

namespace RedDust.Entities
{
    /// <summary>
    /// 实体 GO 生成完成通知。EntityService 在 Spawn 成功后发布，
    /// 其他系统（Camera、UI）订阅以绑定到新 GO。
    /// </summary>
    public readonly struct SEntitySpawned
    {
        public readonly string EntityId;
        public readonly GameObject View;

        public SEntitySpawned(string entityId, GameObject view)
        {
            EntityId = entityId;
            View = view;
        }
    }
}
