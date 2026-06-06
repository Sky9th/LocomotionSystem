using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 伤害效果。duration≤0=瞬时伤害，duration>0=每tick持续伤害（中毒/燃烧）。
    /// 防御公式路由走基类 effectTag（Damage.Elemental.Fire→火抗, Damage.Physical→护甲）。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Ability/Effect/Damage", fileName = "DamageEffect_")]
    public sealed class DamageEffectSO : EffectSO
    {
        [Header("Damage")]
        [Tooltip("基础伤害值。")]
        public float baseDamage;

        [Tooltip("护甲穿透。正=固定穿甲值。")]
        public float armorPenetration;

        [Range(0f, 1f)]
        [Tooltip("护盾穿透。0=盾全额吸收, 1=无视盾。")]
        public float shieldPenetration;

        [Tooltip("经防御后最低伤害。0=可被完全免疫。")]
        public float minDamage;

        [Tooltip("单次伤害上限。0=无上限。")]
        public float maxDamage;
    }
}
