using UnityEngine;

namespace RedDust.Gameplay.Ability
{
    /// <summary>
    /// 伤害通道。只来自实体（武器/身体/陷阱/投掷物/环境），不来自技能。
    /// duration≤0=瞬时伤害，duration>0=每tick持续伤害（中毒/燃烧）。
    /// 防御公式路由走基类 effectTag（Damage.Elemental.Fire→火抗, Damage.Physical→护甲）。
    ///
    /// 技能侧的伤害修正使用 DamageModifierEffectSO。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Effect/Damage", fileName = "DamageEffect_")]
    public sealed class DamageEffectSO : EffectSO
    {
        [Header("Contract")]
        [Tooltip("实体侧：此伤害通道的基底值。来自武器/装备的 PropertyTree Weapon/ATK。")]
        public float baseValue;
    }
}
