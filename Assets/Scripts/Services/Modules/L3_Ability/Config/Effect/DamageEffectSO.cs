using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 伤害效果契约。装备和 Ability 共用同一 asset。
    /// duration≤0=瞬时伤害，duration>0=每tick持续伤害（中毒/燃烧）。
    /// 防御公式路由走基类 effectTag（Damage.Elemental.Fire→火抗, Damage.Physical→护甲）。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Effect/Damage", fileName = "DamageEffect_")]
    public sealed class DamageEffectSO : EffectSO
    {
        [Header("Contract")]
        [Tooltip("装备侧：此伤害通道的基底值。由 GearDefSO 的 outputEffects 填入。")]
        public float baseValue;

        [Tooltip("技能侧：绝对值修正。加法，在乘法前执行。默认 0。")]
        public float modAdd;

        [Tooltip("技能侧：倍率修正。乘法，在加法后执行。默认 1.0。")]
        public float modMult = 1f;

        [Tooltip("执行顺序。同 effectTag 多个 Effect 时按 priority 升序依次叠加。")]
        public int priority;
    }
}
