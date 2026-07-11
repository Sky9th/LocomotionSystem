using RedDust.Gameplay.Ability;
using RedDust.Services.EntityService;
using RedDust.Gameplay.Properties;

namespace RedDust.Gameplay.Equipment
{
    /// <summary>
    /// 武器预设基类。覆盖 GetDamageEffects 从 Weapon/ATK 读 DamageEffectSO[]。
    /// 子类：MeleeWeaponSO（直接读），RangedWeaponSO（沿容器链查弹药 Entity）。
    /// </summary>
    public abstract class WeaponDefSO : EquipmentDefSO
    {
    }
}
