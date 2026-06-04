using System;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 连招衔接定义。当前技能 → 下一技能的单条映射。
    /// SkillDefSO.comboLinks[] 持有，CombatComponent 在 combo window 内匹配。
    ///
    /// 键→技 是角色层（输入映射），技→技衔接是技能层（此 struct）。
    /// </summary>
    [Serializable]
    public struct ComboLink
    {
        /// <summary>可衔接的下一技能。</summary>
        public SkillDefSO nextSkill;

        /// <summary>窗口起始时间（秒），相对当前技能开始。</summary>
        public float windowStart;

        /// <summary>窗口持续时间（秒）。</summary>
        public float windowDuration;

        /// <summary>连招中是否跳过冷却检查。</summary>
        public bool bypassCooldown;
    }
}
