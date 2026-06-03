namespace RedDust.Character.Combat
{
    /// <summary>
    /// 技能目标筛选。Phase 4.2+ 实现过滤逻辑。
    /// </summary>
    public enum ETargetFilter
    {
        /// <summary>任意目标。</summary>
        Any = 0,

        /// <summary>仅敌方。</summary>
        Enemy = 1,

        /// <summary>仅友方。</summary>
        Friendly = 2,

        /// <summary>仅自身。</summary>
        Self = 3,
    }
}
