namespace RedDust.Ability
{
    /// <summary>
    /// 主动技能管道状态。每个值对应一个 State 类。
    ///
    /// Idle → Gating → Search → Cost → Execution → Cooldown → Recovery → Completed
    ///                                     ↑                    │
    ///                                     └── Rejected ←───────┘ (任意步失败)
    /// </summary>
    public enum EActiveAbilityState
    {
        Idle = 0,

        /// <summary>② 门控检查。冷却/互斥/外部条件 — GatingState。</summary>
        Gating = 1,

        /// <summary>③ 搜索命中 — SearchState。</summary>
        Search = 2,

        /// <summary>④ 资源消耗。预检+扣除 — CostState。</summary>
        Cost = 3,

        /// <summary>⑤ 效果载荷 + 逐 hit 结算 — ExecutionState。</summary>
        Execution = 4,

        /// <summary>冷却施加 — CooldownState。</summary>
        Cooldown = 5,

        /// <summary>等待后摇结束 — RecoveryState。</summary>
        Recovery = 6,

        Completed = 7,
        Rejected = 8,
    }
}
