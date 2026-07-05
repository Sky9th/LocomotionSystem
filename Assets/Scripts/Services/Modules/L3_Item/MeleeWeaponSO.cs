using RedDust.Ability;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Items
{
    /// <summary>
    /// 近战武器预设。覆写 GetDamageEffects，从 PropertyTree 的 Weapon/ATK 读取所有 DamageEffectSO。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Entity/Melee Weapon", fileName = "NewMeleeWeapon")]
    public class MeleeWeaponSO : ItemDefSO
    {
        public override EffectSO[] GetDamageEffects(Entity entity)
        {
            return entity?.Properties?.GetAssetList<DamageEffectSO>("Weapon/ATK");
        }
    }
}
