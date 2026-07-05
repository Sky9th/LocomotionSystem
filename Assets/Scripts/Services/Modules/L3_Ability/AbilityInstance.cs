namespace RedDust.Ability
{
    /// <summary>卡片生命周期。</summary>
    public enum ELifecycle
    {
        /// <summary>用完即毁（主动技能）。Pipeline 完成后自动 Deactivate。</summary>
        OneShot,

        /// <summary>常驻（天赋/装备被动）。事件反复触发 FSM，手动 Deactivate。</summary>
        Persistent,

        /// <summary>玩家控制插拔（架势/战吼）。按键插 → 再按键拔。</summary>
        Toggle,
    }

    /// <summary>同一逻辑技能重复激活时的行为。</summary>
    public enum ERefreshPolicy
    {
        /// <summary>刷新已有 instance 的副作用 duration。不叠加效果。</summary>
        Refresh,

        /// <summary>创建新 instance，副作用叠加。</summary>
        Stack,

        /// <summary>先销毁旧的再创建新的。</summary>
        Replace,
    }

    /// <summary>
    /// AbilitySO 的运行时实例。既是身份标识也是副作用 owner（用于 PropertyTable.RemoveAdjuncts）。
    ///
    /// 卡片是其生命周期隐喻：Activate = 插卡，Deactivate = 拔卡。
    ///
    /// Lifecycle 由 Source 推断（当前阶段硬编码）：
    ///   input → OneShot；equipment → Persistent；talent → Persistent；stance → Toggle
    /// </summary>
    public sealed class AbilityInstance
    {
        /// <summary>GUID 短码，调试用。</summary>
        public readonly string Id;

        /// <summary>数据定义（ActiveAbilitySO 或 PassiveAbilitySO）。</summary>
        public readonly AbilitySO Definition;

        /// <summary>
        /// 来源标识。"innate" | equipmentInstance | talent | "input"。
        /// 与 Definition 共同构成判重 key。
        /// </summary>
        public readonly object Source;

        /// <summary>生命周期类型。</summary>
        public ELifecycle Lifecycle;

        /// <summary>重复激活策略。</summary>
        public ERefreshPolicy RefreshPolicy;

        /// <summary>
        /// 是否活跃。Deactivate 置 false，目标侧 FloatState.Tick / CleanupExpiredCooldowns
        /// 检测到此标志后自动清理副作用（Pull 模型）。
        /// </summary>
        public bool IsActive = true;

        public AbilityInstance(AbilitySO definition, object source,
            ELifecycle lifecycle, ERefreshPolicy refreshPolicy = ERefreshPolicy.Refresh)
        {
            Id = System.Guid.NewGuid().ToString("N")[..8];
            Definition = definition;
            Source = source;
            Lifecycle = lifecycle;
            RefreshPolicy = refreshPolicy;
        }

        /// <summary>(Definition, Source) 相同 = 同一"逻辑技能"，用于 RefreshPolicy 判重。</summary>
        public bool IsSameLogicalSkill(AbilityInstance other)
        {
            if (other == null) return false;
            return ReferenceEquals(Definition, other.Definition)
                && Equals(Source, other.Source);
        }

        public override string ToString()
            => $"[{Id}] {Definition?.internalName ?? "?"} ({Source}) | {Lifecycle} | {RefreshPolicy}";
    }
}
