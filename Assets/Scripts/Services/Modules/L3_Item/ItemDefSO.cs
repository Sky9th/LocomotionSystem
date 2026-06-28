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
    ///   身份:    DisplayName (String), Description (String), Icon (AssetRef)
    ///   分类:    ItemTags (GameplayTagList) — Weapon.Blade / Consumable.Medical / Container.Backpack
    ///   属性:    Weight (Float), MaxDurability (Float), MaxStackSize (Int)
    ///   战斗:    ATK (AssetRefList → DamageEffectSO[]), DamageType (GameplayTag)
    ///   效果:    Effects (AssetRefList → EffectSO[])
    ///   表现:    VisualPrefab (AssetRef), AnimationProfile (AssetRef), AudioProfile (AssetRef)
    ///   容器:    CarryWeightMax (Float), CarryVolumeMax (Float)
    ///   槽位:    Common/Slots (Struct → SlotDef[]) — 容器物品通过 OverridesJson 覆写
    ///
    /// 槽位定义（SlotDef）已移至 L3_Container，通过 PropertyType.Struct 在 PropertyTree 中存取。
    /// 运行时：Container&lt;T&gt; 构造时从 PropertyTable.GetStructArray&lt;SlotDef&gt;("Common/Slots") 读取。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Entity/Item", fileName = "NewItem")]
    public class ItemDefSO : PropertyPresetSO
    {
    }
}
