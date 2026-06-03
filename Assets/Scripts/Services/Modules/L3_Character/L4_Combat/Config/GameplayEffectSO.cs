using RedDust.Core;
using UnityEngine;

namespace RedDust.Character.Combat
{
    /// <summary>
    /// 游戏效果定义。统一的持续时间效果——冷却、Buff、Debuff 共享同一类型。
    /// 贯彻架构核心原则："冷却就是对自己施加的 Duration Effect，与 Buff/Debuff 走同一管道。"
    /// 对标 UE GAS UGameplayEffect 的 Duration Policy 层。
    ///
    /// 4.1 仅使用 Duration/GrantedTag（冷却场景）。
    /// Phase 5 引入 Modifiers 和 BlockedTags（Buff/Debuff 场景）。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Combat/Gameplay Effect", fileName = "GE_")]
    public sealed class GameplayEffectSO : ScriptableObject
    {
        [Header("Duration")]
        [Tooltip("持续时间（秒）。≤0 为瞬时效果。")]
        public float duration;

        [Header("Tag")]
        [Tooltip("效果持续期间持有的标签。过期后自动移除。冷却：Skill.Cooldown.X；Buff：Effect.Buff.X。")]
        public GameplayTagDefinitionSO grantedTag;

        [Header("Stacking")]
        [Tooltip("是否可叠加。false 则重复施加时刷新持续时间。")]
        public bool stackable;

        [Tooltip("最大叠加层数。仅 stackable=true 时生效。")]
        public int maxStacks = 1;

        // ── Phase 5 预留 ──

        [Header("Gating (Phase 5+)")]
        [Tooltip("施加条件。任意一个匹配则拒绝施加。")]
        public GameplayTagDefinitionSO[] applicationBlockedTags;

        // [Header("Modifiers (Phase 5+)")]
        // public StatModifier[] modifiers;
    }
}
