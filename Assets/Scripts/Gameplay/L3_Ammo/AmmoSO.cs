using RedDust.Ability;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Ammo
{
    /// <summary>
    /// 弹药预设。覆写 GetDamageEffects 从 PropertyTree Weapon/ATK 读 DamageEffectSO。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Entity/Ammo", fileName = "NewAmmo")]
    public class AmmoSO : AmmoDefSO
    {
        public override EffectSO[] GetDamageEffects(Entity entity)
        {
            return entity?.Properties?.GetAssetList<DamageEffectSO>("Weapon/ATK");
        }
    }
}
