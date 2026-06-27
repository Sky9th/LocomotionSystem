namespace RedDust.Entities
{
    /// <summary>
    /// 实体 GO 已销毁通知。EntityService 在 Despawn 后发布。
    /// </summary>
    public readonly struct SEntityDespawned
    {
        public readonly string EntityId;

        public SEntityDespawned(string entityId)
        {
            EntityId = entityId;
        }
    }
}
