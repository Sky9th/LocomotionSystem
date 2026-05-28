namespace RedDust.Stats
{
    /// <summary>
    /// 无 Tick 被动触发的能力（击杀得经验、采集得材料等）。
    /// 只标记可被外部 Modify 累积，不参与自动 Tick。
    /// </summary>
    public interface IStatCumulative { }
}
