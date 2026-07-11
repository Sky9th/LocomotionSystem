namespace RedDust.Ability
{
    /// <summary>
    /// 主动技能管道状态。每个值对应一个 State 类。
    ///
    /// Gating → Cost → Activation → Cooldown → Execution → Recovery → Completed
    ///                                                      ↑                    │
    ///                                                      └── Rejected ←───────┘ (任意步失败)
    /// </summary>
    public enum EActiveAbilityState
    {

        /// <summary>② 门控检查。冷却/互斥/外部条件 — GatingState。</summary>
        Gating = 1,

        /// <summary>③ 前摇计时。等待 windupDuration / animationSpeed — WindupState。</summary>
        Windup = 2,

        /// <summary>⑤ 资源消耗。预检+扣除 — CostState。</summary>
        Cost = 4,

        /// <summary>⑥ 效果载荷 + 逐 hit 结算 — ExecutionState。</summary>
        Execution = 5,

        /// <summary>冷却施加 — CooldownState。</summary>
        Cooldown = 6,

        /// <summary>等待后摇结束 — RecoveryState。</summary>
        Recovery = 7,

        Completed = 8,
        Rejected = 9,
    }
}
