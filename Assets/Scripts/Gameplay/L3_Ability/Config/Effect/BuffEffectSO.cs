using RedDust.Core.RdTag;
using System;
using RedDust.Core.Events;
using RedDust.Gameplay.Properties;
using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// Buff/Debuff 效果。向目标注入 FloatAdjunct（只读修正） + RdTag，
    /// duration 秒后全部自动还原。
    ///
    /// 运行时翻译：读 BuffEffectSO → 拼 FloatAdjunct → PropertyTable.AddAdjunct。
    /// 过期：FloatAdjunct.ExpiryTime 到期后 FloatState.Tick 自动清理。
    /// Tags 由 AbilityComponent._buffTags 跟踪，Tick 中清理。
    ///
    /// 用法范本：
    /// - 临时 Buff：ActiveAbilitySO.selfEffects, duration=2s → 压制射击减速
    /// - 天赋（永久 Buff）：PassiveAbilitySO(OnEquip).selfEffects, duration≤0 → 铁壁
    /// - 条件 Buff：PassiveAbilitySO(OnDamaged).targetEffects, duration=5s → 受伤减伤
    /// - 装备 Buff：GearDefSO.outputEffects, duration≤0 → 防具护甲
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Effect/Buff", fileName = "BuffEffect_")]
    public sealed class BuffEffectSO : EffectSO
    {
        [Header("Buff")]
        [Tooltip("持续期间写入 OwnedTags 的标签。通过 AbilityComponent._buffTags 管理过期。")]
        public RdTagDefSO[] grantedTags;

        [Tooltip("持续期间的 FloatAdjunct 模板。运行时翻译为 FloatAdjunct 注入 Properties。")]
        public SBuffAdjunct[] adjuncts;
    }

    /// <summary>
    /// Buff 的属性修正模板。对应一条 FloatAdjunct 实例。
    /// 堆叠规则：valueAdd 按层数线性叠加（×stackCount），valueMultiply 不随层数变化。
    /// maxAdd/maxMultiply 用于扩展属性上界（如 +20% MaxHP）。
    /// </summary>
    [Serializable]
    public struct SBuffAdjunct
    {
        [Tooltip("修改哪个属性。TargetPath = property.Id。")]
        public PropertyDefSO property;

        [Tooltip("固定偏移量。正=增益, 负=减益。同 Buff 叠层时 ×stackCount。")]
        public float valueAdd;

        [Tooltip("乘数。1=不变, 0.7=减速30%, 1.3=加速30%。不随层数叠加。")]
        public float valueMultiply;

        [Tooltip("Max 固定偏移。正=扩容, 负=缩限。不随层数叠加。")]
        public float maxAdd;

        [Tooltip("Max 乘数。1=不变, 1.2=+20%上限。不随层数叠加。")]
        public float maxMultiply;
    }
}
