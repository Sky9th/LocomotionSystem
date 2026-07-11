namespace RedDust.Properties
{
    /// <summary>
    /// 只读修正。挂在 FloatState 上，读取 Effective 时叠加，不改 Current。
    /// 移除即还原——原始值无痕。
    ///
    /// ValueAdd/ValueMultiply 是静态偏移/乘数，不是速率修改器。
    /// 要改消耗/恢复速度（如"50% 更慢的饥饿消耗"），使用 FloatModifier.OnApplyRate。
    ///
    /// MaxAdd/MaxMultiply 允许扩展钳制上界（如 +20% MaxHP）。
    /// 不影响 Min——Min 扩展由 MinAdd/MinMultiply 控制（罕用）。
    ///
    /// ExpiryTime = -1 为永久（天赋/装备），>0 到期后 FloatState.Tick 自动清理。
    /// </summary>
    public class FloatAdjunct
    {
        /// <summary>所属者。用于批量移除。</summary>
        public object Owner;

        /// <summary>属性路径，如 "MoveSpeed"。</summary>
        public string TargetPath;

        /// <summary>固定偏移。正=增益, 负=减益。</summary>
        public float ValueAdd;

        /// <summary>乘数。1=不变, 0.7=减速30%, 1.3=加速30%。</summary>
        public float ValueMultiply = 1f;

        /// <summary>Max 固定偏移。正=扩容, 负=缩限。</summary>
        public float MaxAdd;

        /// <summary>Max 乘数。1=不变, 1.2=+20%上限。</summary>
        public float MaxMultiply = 1f;

        /// <summary>过期时间（Time.time 基准）。-1 = 永久。</summary>
        public float ExpiryTime = -1f;
    }
}
