using Animancer;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 单个技能的完整数据定义。纯配置，无运行时状态。
    /// 三层架构中的配置层，被 CombatComponent / CombatPipeline / CombatDriver 消费。
    ///
    /// 对标 UE GAS：UGameplayAbility 蓝图 + 关联的 Cooldown GE / Cost GE。
    /// 关键差异：冷却不内联 duration+tag，而是引用 GameplayEffectSO——
    /// 冷却和 Buff/Debuff 是同一概念，走同一管道。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Combat/Skill Definition", fileName = "Skill_")]
    public sealed class SkillDefSO : ScriptableObject
    {
        [Header("Identity")]
        public string skillName;
        public string displayName;

        [Header("Tag Gating")]
        [Tooltip("激活受阻标签。任意一个层级匹配则拒绝激活。")]
        public GameplayTagDefinitionSO[] activationBlockedTags;

        [Tooltip("激活期间持有的标签。技能结束时自动移除。")]
        public GameplayTagDefinitionSO[] abilityTags;

        [Header("Cooldown")]
        [Tooltip("冷却效果。duration>0 且 grantedTag 非空时生效。激活后施加，过期自动移除标签。")]
        public GameplayEffectSO cooldownEffect;

        [Header("Resource Cost")]
        [Tooltip("体力消耗。≤0 则无消耗。Phase 5 升级为 GameplayEffectSO 数组以支持多种消耗。")]
        public float staminaCost;

        [Header("Hit Detection")]
        public ECombatSearchType searchType;

        [Tooltip("搜索半径。Cone/Circle 为球形半径，RayLine 为射线长度。")]
        public float searchRange;

        [Range(0f, 360f)]
        [Tooltip("搜索角度（仅 Cone 使用）。")]
        public float searchAngle = 90f;

        [Tooltip("最大命中目标数。≤0 无限制。")]
        public int maxTargets;

        [Tooltip("是否需要视线（仅 RayLine 使用）。")]
        public bool requiresLineOfSight;

        [Tooltip("物理层遮罩。")]
        public LayerMask targetMask = ~0;

        [Header("Animation")]
        public SkillAnimationLayer animationLayer;

        [Tooltip("Animancer StringAsset 引用。")]
        public StringAsset animationAlias;

        [Header("Phase Timing")]
        [Tooltip("前摇持续时间（秒）。")]
        public float windupDuration = 0.05f;

        [Tooltip("激发窗口持续时间（秒）。此期间每帧执行命中检测。")]
        public float fireWindowDuration = 0.2f;

        [Header("Damage")]
        [Tooltip("伤害倍率。基础伤害 × 此值 = 最终伤害。")]
        public float damageMultiplier = 1f;

        [Header("Noise")]
        [Tooltip("噪音等级。≤0 不产生噪音。")]
        public float noiseLevel;

        [Tooltip("噪音类型标签。HumanActivity / WeaponExplosion 等。")]
        public GameplayTagDefinitionSO noiseType;

        // ── Combo（Phase 4.1b 实现，4.1 字段占位）──

        [Header("Combo (Phase 4.1b)")]
        public float comboWindowStart;
        public float comboWindowDuration;
        public int[] comboNextSlots;
        public bool[] comboBypassCooldowns;
    }
}
