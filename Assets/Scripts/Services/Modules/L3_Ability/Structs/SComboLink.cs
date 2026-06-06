using System;

namespace RedDust.Ability
{
    /// <summary>
    /// 连招衔接定义。当前技能 → 下一技能的单条映射。
    /// AbilityDefSO.comboLinks[] 持有，AbilityExecutor 在 combo window 内匹配。
    ///
    /// 键→技 是角色层（输入映射），技→技衔接是技能层（此 struct）。
    /// </summary>
    [Serializable]
    public struct SComboLink
    {
        /// <summary>可衔接的下一技能。</summary>
        public AbilityDefSO NextSkill;

        /// <summary>窗口起始时间（秒），相对当前技能开始。</summary>
        public float WindowStart;

        /// <summary>窗口持续时间（秒）。</summary>
        public float WindowDuration;

        /// <summary>连招中是否跳过冷却检查。</summary>
        public bool BypassCooldown;
    }
}
