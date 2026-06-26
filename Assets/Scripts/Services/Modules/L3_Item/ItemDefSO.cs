using System;
using UnityEngine;
using RedDust.Core;
using RedDust.Properties;

namespace RedDust.Items
{
    /// <summary>
    /// 容器槽位定义——描述物品提供的一个独立容纳空间。
    /// 结构体聚合（非标量），因此留在 C# 不在 PropertyTree。
    /// </summary>
    [Serializable]
    [PropertyStruct]
    public struct SlotDef
    {
        /// <summary>槽位标识，同一物品内唯一。——"Main", "WeaponSling", "WaterPouch"</summary>
        [Tooltip("槽位标识，同一物品内唯一。")]
        public string SlotId;

        /// <summary>此槽位接受什么类型的物品。匹配候选物品的 ItemTags。空数组 = 接受所有物品。</summary>
        [Tooltip("此槽位接受什么类型的物品。匹配候选物品的 ItemTags。空 = 接受所有。")]
        public GameplayTagDefinitionSO[] AcceptTags;

        /// <summary>槽位容量（物品数量上限）。</summary>
        [Tooltip("槽位容量（物品数量上限）。")]
        [Min(1)]
        public int Capacity;

        /// <summary>此槽位内物品的总重量上限。0 = 无限制。</summary>
        [Tooltip("此槽位内物品的总重量上限。0 = 无限制。")]
        [Min(0f)]
        public float WeightLimit;
    }

    /// <summary>
    /// 物品定义资产。继承 PropertyPresetSO。
    ///
    /// 所有叶子数据进 PropertyTree（Template + OverridesJson）：
    ///   身份:    DisplayName (String), Description (String), Icon (AssetRef)
    ///   分类:    ItemTags (GameplayTagList) — Weapon.Blade / Consumable.Medical / Container.Backpack
    ///   属性:    Weight (Float), Durability (Float), MaxDurability (Float), StackSize (Int)
    ///   战斗:    ATK (AssetRefList → DamageEffectSO[]), DamageType (GameplayTag)
    ///   效果:    Effects (AssetRefList → EffectSO[])
    ///   表现:    VisualPrefab (AssetRef), AnimationProfile (AssetRef), AudioProfile (AssetRef)
    ///   容器:    CarryWeightMax (Float), CarryVolumeMax (Float)
    ///
    /// 唯一 C# 字段：Slots（SlotDef[]）。
    ///   结构化数据——聚合体，数组表达多槽位。
    ///   不进 PropertyTree：PropertyTree 是 key→标量映射，不表达结构体内聚。
    ///   不做独立 SO：槽位配置 1:1 专属物品，无复用场景，不值得独立资产化。
    ///   和 PropertyPresetSO 基类注释一致——"子类追加机械规则字段（slots, spawnBehavior）"。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Item/ItemDef", fileName = "NewItem")]
    public class ItemDefSO : PropertyPresetSO
    {
        /// <summary>
        /// 此物品提供的容器槽位。空数组 = 不可容纳其他物品（消耗品、弹药、非容器装备）。
        /// 非空 = 容器物品（背包、枪械 Receiver、载具后备箱）。
        ///
        /// 运行时创建 Container 时直接 foreach 遍历此数组：
        ///   foreach (var slot in def.Slots)
        ///       container.AddSlot(slot.SlotId, slot.AcceptTags, slot.Capacity, slot.WeightLimit);
        /// O(n) 数组遍历，零字符串拼接，零字典查找。
        /// </summary>
        [Tooltip("此物品提供的容器槽位。空数组 = 不可容纳其他物品。非空 = 容器物品。")]
        public SlotDef[] Slots = Array.Empty<SlotDef>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Slots is not { Length: > 1 }) return;

            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < Slots.Length; i++)
            {
                if (string.IsNullOrEmpty(Slots[i].SlotId))
                {
                    Debug.LogWarning($"[ItemDefSO] {name}: Slots[{i}] SlotId is empty.", this);
                    continue;
                }
                if (!seen.Add(Slots[i].SlotId))
                {
                    Debug.LogError($"[ItemDefSO] {name}: Duplicate SlotId '{Slots[i].SlotId}' at index {i}.", this);
                }
            }
        }
#endif
    }
}
