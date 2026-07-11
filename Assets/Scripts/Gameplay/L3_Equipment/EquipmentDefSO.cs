using RedDust.Gameplay.Properties;

namespace RedDust.Gameplay.Equipment
{
    /// <summary>
    /// 装备预设基类。Weapon / Armor / Tool / Container 的共同父类。
    /// PropertyTree 父模板 = Equipment（Durability, VisualPrefab, AnimationProfile, AudioProfile）。
    /// </summary>
    public abstract class EquipmentDefSO : PropertyPresetSO
    {
    }
}
