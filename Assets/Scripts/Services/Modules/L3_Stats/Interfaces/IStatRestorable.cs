namespace RedDust.Stats
{
    /// <summary>
    /// 按间隔持续恢复的能力。
    /// </summary>
    public interface IStatRestorable
    {
        float Rate { get; }
        float Interval { get; }
    }
}
