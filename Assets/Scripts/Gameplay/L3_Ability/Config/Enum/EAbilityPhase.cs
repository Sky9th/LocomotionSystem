namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// 技能动画驱动阶段。AbilityDriver 内部状态机使用。
    /// 4.1 仅使用 None→Windup→Fire→Recovery，Active/Cancelled 预留。
    /// </summary>
    public enum EAbilityPhase
    {
        /// <summary>无活跃技能。</summary>
        None = 0,

        /// <summary>前摇阶段。播放起手动画。</summary>
        Windup = 1,

        /// <summary>持续/循环阶段。等待输入或计时。Phase 4.1b+ 实现。</summary>
        Active = 2,

        /// <summary>激发阶段。执行命中检测、施加伤害。</summary>
        Fire = 3,

        /// <summary>后摇阶段。动画收招。</summary>
        Recovery = 4,

        /// <summary>被取消。主动取消或被中断。Phase 4.1b+ 实现。</summary>
        Cancelled = 5
    }
}
