namespace RedDust.Ability
{
    /// <summary>
    /// 主动技能管道状态。每个值对应一个 State 类。
    ///
    /// Idle → Gating → Cost → Execution → Cooldown → Recovery → Completed
    ///                                   ↑                    │
    ///                                   └── Rejected ←───────┘ (任意步失败)
    /// </summary>
    public enum EActiveAbilityState
    {
        Idle = 0,

        /// <summary>② 门控检查。冷却/互斥/外部条件 — GatingState。</summary>
        Gating = 1,

        /// <summary>③ 资源消耗。预检+扣除 — CostState。</summary>
        Cost = 2,

        /// <summary>④⑤ 搜索命中 + 效果载荷 — ExecutionState。</summary>
        Execution = 3,

        /// <summary>冷却施加 — CooldownState。</summary>
        Cooldown = 4,

        /// <summary>等待后摇结束 — RecoveryState。</summary>
        Recovery = 5,

        Completed = 6,
        Rejected = 7,
    }
}
