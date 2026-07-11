using System;
using UnityEngine;
using RedDust.Properties;

namespace RedDust.Container
{
    /// <summary>
    /// 容器槽位定义——描述一个独立容纳空间。
    ///
    /// 通过 PropertyType.Struct 存入 PropertyTree，每个槽位为 Slots/ 文件夹下独立 Struct 属性，
    /// NodeId 即 SlotId（由 CharacterContainer.OnWire 从路径末段提取并回填）。
    ///
    /// AcceptTags 改为 string[]（Tag 全路径），兼容 JSON 序列化/反序列化。
    /// </summary>
    [Serializable]
    [PropertyStruct]
    public struct SlotDef
    {
        /// <summary>
        /// 槽位标识，同一容器内唯一。不在 JSON 中存储，
        /// 由 CharacterContainer.OnWire 从属性路径末段提取后回填。
        /// </summary>
        public string SlotId;

        /// <summary>
        /// 此槽位接受什么类型的物品。匹配候选物品的 ItemTags。空或 null = 接受所有物品。
        /// 存储 RdTag 全路径字符串（如 "Weapon"、"Armor.Head"），兼容 JSON 序列化。
        /// </summary>
        [Tooltip("此槽位接受什么类型的物品。匹配候选物品的 ItemTags。空 = 接受所有。")]
        public string[] AcceptTags;

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
