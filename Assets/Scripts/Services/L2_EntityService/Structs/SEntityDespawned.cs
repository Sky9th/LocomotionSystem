using UnityEngine;

namespace RedDust.Services.EntityService
{
    /// <summary>
    /// 实体 GO 已销毁通知。EntityService 在 Despawn 后发布。
    /// View 是销毁前的 GO 引用——消费者用此清空缓存。
    /// </summary>
    public readonly struct SEntityDespawned
    {
        public readonly string EntityId;
        public readonly GameObject View;

        public SEntityDespawned(string entityId, GameObject view)
        {
            EntityId = entityId;
            View = view;
        }
    }
}
