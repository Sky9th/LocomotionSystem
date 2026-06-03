using RedDust.Core;
using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 单个技能的完整数据定义。纯配置，无运行时状态。
    /// 重构中——逐类分析字段归属。标记说明见下方。
    ///
    /// ✅ Confirmed : 已确认属于 SkillDef
    /// 🔶 Pending   : 待逐类分析讨论
    /// 🚫 Moved     : 已迁出到子资产（如 SkillActivationSO）
    ///
    /// 三层架构中的配置层，被 CombatComponent / CombatPipeline / CombatDriver 消费。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Combat/Skill Definition", fileName = "Skill_")]
    public sealed class SkillDefSO : ScriptableObject
    {
        // ════════════════════════════════════════════════════════════════
        // ✅ Confirmed — 技能身份（"技能是什么"）
        // ════════════════════════════════════════════════════════════════

        [Header("✅ Identity")]
        public string skillName;
        public string displayName;
        public Sprite icon;
        [TextArea(2, 4)]
        public string description;
        public GameplayTagDefinitionSO categoryTag;


        // ════════════════════════════════════════════════════════════════
        // 🚫 Moved → SkillActivationSO
        // ════════════════════════════════════════════════════════════════

        [Header("🚫 Activation (moved to SkillActivationSO)")]
        [Tooltip("技能「怎么放」的完整定义。动画、阶段时机、激活方式、连招。")]
        public SkillActivationSO activation;


        // ════════════════════════════════════════════════════════════════
        // 🔶 Pending — 待逐类分析讨论
        // ════════════════════════════════════════════════════════════════

        [Header("🔶 Tag Gating")]
        public GameplayTagDefinitionSO[] activationBlockedTags;
        public GameplayTagDefinitionSO[] abilityTags;

        [Header("🔶 Cooldown")]
        public GameplayEffectSO cooldownEffect;

        [Header("🔶 Resource Cost")]
        public float staminaCost;

        [Header("🚫 Search (moved to SkillSearchSO)")]
        [Tooltip("技能「往哪打」的完整定义。搜索形状、范围、目标筛选。")]
        public SkillSearchSO search;

        [Header("🔶 Damage")]
        public float damageMultiplier = 1f;

        [Header("🔶 Noise")]
        public float noiseLevel;
        public GameplayTagDefinitionSO noiseType;

        [Header("🔶 Combo (Phase 4.1b)")]
        public float comboWindowStart;
        public float comboWindowDuration;
        public int[] comboNextSlots;
        public bool[] comboBypassCooldowns;

        // ── 以下类别尚未分析，暂未加入字段 ──
        // 2. 目标筛选 (仅敌/仅友/任意/仅自身)
        // 3. 击退/硬直/处决
        // 4. 投射物
        // 5. 位移
        // 6. 召唤
        // 7. 视觉/音频
        // 8. AI 使用提示
    }
}
