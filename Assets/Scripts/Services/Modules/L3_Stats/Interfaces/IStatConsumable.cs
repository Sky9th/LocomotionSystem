namespace RedDust.Stats
{
    /// <summary>
    /// 按间隔持续扣减的能力。
    /// 实现了此接口的 StatDefinitionSO 会在 StatInstance.Tick 中自动消耗。
    /// Interval = 0 时每帧扣除。
    /// </summary>
    public interface IStatConsumable
    {
        float Rate { get; }
        float Interval { get; }
    }
}
