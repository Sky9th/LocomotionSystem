using RedDust.Core;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 伤害修正效果。技能侧使用，按 targetTag 匹配实体伤害通道并修正。
    ///
    /// 伤害公式：outgoing = baseValue × (1 + ΣmodPercent) + ΣmodAdd
    /// 多个 modifier 对同一通道：百分比加法叠加，固定值在乘法后叠加。
    /// 不会出现 ×3×4 爆炸。
    ///
    /// 与 DamageEffectSO 的区别：
    ///   DamageEffectSO       — 伤害通道，来自实体（武器/身体），携带 baseValue
    ///   DamageModifierEffectSO — 伤害修正，来自技能，修正匹配 tag 的通道
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Effect/Damage Modifier", fileName = "DamageMod_")]
    public sealed class DamageModifierEffectSO : EffectSO
    {
        [Header("Target")]
        [Tooltip("要修正哪个 tag 的伤害通道。与实体通道的 effectTag 精确匹配。")]
        public RdTagDefSO targetTag;

        [Header("Modifier")]
        [Tooltip("固定值加成。在乘法后叠加，不受百分比加成影响。")]
        public float modAdd;

        [Tooltip("百分比加成。0.5=+50%, 1.0=+100%。多个 modifier 加法叠加：baseValue × (1 + ΣmodPercent)。")]
        public float modPercent;

        [Tooltip("叠加顺序。同 targetTag 多 modifier 时按 priority 升序执行（仅影响 modAdd 的叠加顺序）。")]
        public int priority;
    }
}
