namespace RedDust.Ability
{
    /// <summary>
    /// 主动技能管道状态机。技能释放是连续帧过程，由 Tick 逐帧驱动。
    ///
    /// Idle → CanEnter → BeforeExe → Execute → AfterExe → CanExit → Completed
    ///                                     ↑                    │
    ///                                     └── Rejected ←───────┘ (任意步失败)
    /// </summary>
    public enum EActiveAbilityState
    {
        /// <summary>未执行。</summary>
        Idle = 0,

        /// <summary>② 门控检查。冷却/互斥/外部条件，一帧完成。</summary>
        CanEnter = 1,

        /// <summary>③ 资源消耗。预检+扣除，一帧完成。</summary>
        BeforeExe = 2,

        /// <summary>Windup → Fire(④Search+⑤Effects) → Recovery。多帧驱动。</summary>
        Execute = 3,

        /// <summary>冷却施加 + 清理，一帧完成。</summary>
        AfterExe = 4,

        /// <summary>等待后摇动画结束，多帧。</summary>
        CanExit = 5,

        /// <summary>管道完成。</summary>
        Completed = 6,

        /// <summary>被门控或资源检查拒绝。</summary>
        Rejected = 7,
    }
}
