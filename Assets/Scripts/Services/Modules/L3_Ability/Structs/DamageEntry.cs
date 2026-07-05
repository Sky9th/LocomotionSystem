using RedDust.Core;

namespace RedDust.Ability
{
    /// <summary>
    /// 单通道伤害数据。每个 DamageEntry 对应一个实体伤害通道（武器/身体），
    /// 经过技能修正后的 outgoing 伤害值。
    ///
    /// Duration≤0 = 瞬时伤害，>0 = DOT（当前不落地，需 FloatModifier ExpiryTime）。
    /// </summary>
    public readonly struct DamageEntry
    {
        /// <summary>伤害类型标签。用于 Reactor 侧按 tag 路由抗性。</summary>
        public readonly RdTag Tag;

        /// <summary>施展方 outgoing 伤害（目标减免前）。</summary>
        public readonly float Amount;

        /// <summary>0 = 瞬时伤害，>0 = DOT 持续时长（秒）。</summary>
        public readonly float Duration;

        /// <summary>DOT 跳间隔（秒）。0 = 未指定。</summary>
        public readonly float Interval;

        public DamageEntry(RdTag tag, float amount, float duration = 0f, float interval = 0f)
        {
            Tag = tag;
            Amount = amount;
            Duration = duration;
            Interval = interval;
        }

        public bool IsInstant => Duration <= 0f;
        public bool IsDot => Duration > 0f;
    }
}
