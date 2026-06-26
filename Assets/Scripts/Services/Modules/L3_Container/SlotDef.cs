using System;
using UnityEngine;
using RedDust.Core;
using RedDust.Properties;

namespace RedDust.Container
{
    /// <summary>
    /// 容器槽位定义——描述一个独立容纳空间。
    ///
    /// 通过 PropertyType.Struct 存入 PropertyTree（Common/Slots），
    /// 运行时由 Container 构造时 foreach 遍历创建 ContainerSlot。
    ///
    /// 归属 L3_Container——Container 是 SlotDef 的主要运行时消费者，
    /// PropertyTree 仅负责存储/反序列化。
    /// </summary>
    [Serializable]
    [PropertyStruct]
    public struct SlotDef
    {
        /// <summary>槽位标识，同一容器内唯一。——"RightHand", "Main", "WeaponSling"</summary>
        [Tooltip("槽位标识，同一容器内唯一。")]
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
}
