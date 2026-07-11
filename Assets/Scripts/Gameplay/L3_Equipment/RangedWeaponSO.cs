using RedDust.Gameplay.Ability;
using RedDust.Gameplay.Container;
using RedDust.Services.EntityService;
using UnityEngine;

namespace RedDust.Gameplay.Equipment
{
    /// <summary>
    /// 远程武器预设。伤害来自弹药 Entity——沿容器链向下查找弹药，返回其 DamageEffectSO。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Entity/Ranged Weapon", fileName = "NewRangedWeapon")]
    public class RangedWeaponSO : WeaponDefSO
    {
        public override EffectSO[] GetDamageEffects(Entity entity)
        {
            if (entity == null) return null;

            var ammo = FindAmmoInContainers(entity);
            if (ammo != null)
            {
                var effects = ammo.Preset?.GetDamageEffects(ammo);
                if (effects != null && effects.Length > 0)
                    return effects;
            }

            return null;
        }

        /// <summary>
        /// 递归沿容器链查找弹药 Entity。深度优先，最大深度 10。
        /// </summary>
        private static Entity FindAmmoInContainers(Entity entity)
        {
            return FindAmmoRecursive(entity, 0);
        }

        private static Entity FindAmmoRecursive(Entity entity, int depth)
        {
            if (entity == null || depth > 10) return null;
            var container = entity.NestedContainer;
            if (container == null) return null;

            foreach (var item in container.AllItems())
            {
                if (item == null) continue;
                if (item.Preset is Ammo.AmmoDefSO)
                    return item;
                var found = FindAmmoRecursive(item, depth + 1);
                if (found != null) return found;
            }

            return null;
        }
    }
}
