using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Container
{
    /// <summary>
    /// 单个槽位的运行时状态。由 Container&lt;T&gt; 持有和管理。
    /// </summary>
    public class ContainerSlot<T>
    {
        /// <summary>不可变配置（来自 PropertyTree Struct 节点）。</summary>
        public SlotDef Def { get; }

        /// <summary>当前容纳的物品。</summary>
        public List<T> Items { get; } = new();

        /// <summary>槽内物品总重缓存。</summary>
        public float CurrentWeight { get; private set; }

        public bool IsFull => Items.Count >= Def.Capacity;
        public bool IsEmpty => Items.Count == 0;

        public ContainerSlot(SlotDef def)
        {
            Def = def;
        }

        /// <summary>
        /// 检查物品是否可放入此槽位。
        /// 顺序：存在性 → 容量 → AcceptTags → WeightLimit。
        /// </summary>
        public bool CanAccept(T item)
        {
            if (item == null) return false;

            // 容量检查
            if (IsFull) return false;

            // TODO ItemInstance 到位后接入:
            //   Tag 过滤: item.ItemTags ∩ Def.AcceptTags 非空
            //   重量检查: CurrentWeight + item.Weight > Def.WeightLimit

            return true;
        }

        /// <summary>
        /// 放入物品。先调 CanAccept，失败返回 false。
        /// </summary>
        public bool Place(T item)
        {
            if (!CanAccept(item)) return false;

            Items.Add(item);
            // TODO ItemInstance 到位后: CurrentWeight += item.Weight
            return true;
        }

        /// <summary>
        /// 按引用移除物品。未找到返回 false。
        /// </summary>
        public bool Remove(T item)
        {
            if (!Items.Remove(item)) return false;
            // TODO ItemInstance 到位后: CurrentWeight -= item.Weight
            return true;
        }

        /// <summary>
        /// 按 itemId 移除物品。未找到返回 default。
        /// ItemInstance 到位前，T 无 Id 属性——仅按引用移除。
        /// </summary>
        public T Remove(string itemId)
        {
            // TODO ItemInstance 到位后: 按 item.Id 匹配
            Debug.LogWarning($"[ContainerSlot] Remove(string) not supported without ItemInstance. Use Remove(T) instead.");
            return default;
        }
    }
}
