using RedDust.Ability;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Items
{
    /// <summary>
    /// 近战武器预设。覆写 GetDamageEffect，从 PropertyTree 的 Weapon/ATK 读取 DamageEffectSO。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Entity/Melee Weapon", fileName = "NewMeleeWeapon")]
    public class MeleeWeaponSO : ItemDefSO
    {
        public override DamageEffectSO GetDamageEffect(Entity entity)
        {
            if (entity?.Properties == null) return null;
            var effects = entity.Properties.GetAssetList<DamageEffectSO>("Weapon/ATK");
            return effects?.Length > 0 ? effects[0] : null;
        }
    }
}
