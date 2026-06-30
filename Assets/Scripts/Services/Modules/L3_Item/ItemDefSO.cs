using System;
using UnityEngine;
using RedDust.Core;
using RedDust.Properties;

namespace RedDust.Items
{
    /// <summary>
    /// 物品定义资产。继承 PropertyPresetSO，零 C# 字段。
    ///
    /// 所有数据全进 PropertyTree（Template + OverridesJson）：
    ///   通用:    Common/Weight (Float), Common/MaxStackSize (Float), Common/Tags (rTagList)
    ///   战斗:    Combat/ATK (AssetRefList → DamageEffectSO[])
    ///   槽位:    Common/Slots (Struct → SlotDef[]) — 容器物品通过 OverridesJson 覆写
    ///
    /// 武器子类：MeleeWeaponSO 覆写 GetDamageEffect → 从 Combat/ATK 读取 DamageEffectSO。
    /// RangedWeaponSO 沿容器链递归查询弹药 Entity 的 DamageEffectSO。
    ///
    /// 槽位定义（SlotDef）已移至 L3_Container，通过 PropertyType.Struct 在 PropertyTree 中存取。
    /// 运行时：Container&lt;T&gt; 构造时从 PropertyTable.GetStructArray&lt;SlotDef&gt;("Common/Slots") 读取。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Entity/Item", fileName = "NewItem")]
    public class ItemDefSO : PropertyPresetSO
    {
    }
}
