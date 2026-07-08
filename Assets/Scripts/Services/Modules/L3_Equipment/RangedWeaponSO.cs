using RedDust.Ability;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Equipment
{
    /// <summary>
    /// 远程武器预设。伤害来自弹药 Entity，而非武器自身。
    /// TODO: 沿容器链递归向下查找弹药 Entity，返回弹药的 DamageEffectSO。
    /// 1911 → NestedContainer["Magazine"] → magazineEntity
    ///      → magazineEntity.NestedContainer["Ammo"] → ammoEntity
    ///      → ammoEntity.Preset.GetDamageEffect(ammoEntity)
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Entity/Ranged Weapon", fileName = "NewRangedWeapon")]
    public class RangedWeaponSO : WeaponDefSO
    {
        public override EffectSO[] GetDamageEffects(Entity entity)
        {
            // TODO: 临时模拟——递归查弹药容器链拿到弹药 Entity 后，调 ammoEntity.Preset.GetDamageEffects。
            var fake = ScriptableObject.CreateInstance<DamageEffectSO>();
            fake.name = "TEMPORARY_RangedDamage";
            fake.baseValue = 10f;
            return new EffectSO[] { fake };
        }
    }
}
