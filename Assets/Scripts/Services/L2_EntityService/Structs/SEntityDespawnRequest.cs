namespace RedDust.Services.EntityService
{
    /// <summary>
    /// 请求销毁实体 GO（Entity 数据保留）。由外部系统发布，
    /// EntityService 订阅并执行 Despawn。
    /// </summary>
    public readonly struct SEntityDespawnRequest
    {
        public readonly string EntityId;

        public SEntityDespawnRequest(string entityId)
        {
            EntityId = entityId;
        }
    }
}
