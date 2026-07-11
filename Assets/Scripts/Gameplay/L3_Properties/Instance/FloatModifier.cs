using System;

namespace RedDust.Gameplay.Properties
{
    /// <summary>速率上下文。修改器通过 OnApplyRate 修改 Addend / Multiplier 影响 Tick 速率。</summary>
    public class RateContext
    {
        public float Addend;
        public float Multiplier = 1f;
    }

    /// <summary>预设修改频率。</summary>
    public enum ModifierFrequency
    {
        PerFrame,   // 每帧
        PerSecond,  // 每秒（共享计时器）
        PerMinute,  // 每分钟（共享计时器）
        Custom      // 自定义间隔（自维护计时器，慎用）
    }

    /// <summary>
    /// 持久帧级浮点修改器。由 Properties 模块定义标准，所有子系统遵守此约定注入。
    /// 三类修改方式：A — OnApplyRate（速率影响）、B — Delta（定时直接修改）、C — CustomTick（完全接管）。
    /// </summary>
    public class FloatModifier
    {
        public object Owner;
        public string TargetPath;

        /// <summary>执行频率。</summary>
        public ModifierFrequency Frequency;

        /// <summary>仅当 Frequency == Custom 时生效，自定义间隔（秒）。</summary>
        public float CustomInterval;

        // A — 速率影响
        /// <summary>每 Tick 修改消耗/恢复的 Addend / Multiplier。例：ctx.Multiplier = 3f → 3 倍消耗。</summary>
        public Action<RateContext> OnApplyRate;

        // B — 定时直接修改
        /// <summary>每次执行的修改量，正增负减。例：Delta=-5 → 每次扣 5。</summary>
        public float Delta;
        /// <summary>执行条件，null = 无条件。</summary>
        public Func<bool> Condition;

        // C — 自定义
        /// <summary>完全接管 Tick。无法用 A+B 表达的极端场景。</summary>
        public Action<FloatState, float> CustomTick;
    }
}
