using System.Collections.Generic;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Container
{
    /// <summary>
    /// 单个槽位的运行时状态。由 Container 持有和管理。
    /// 存放 Entity 引用，Tag/Weight 过滤从 PropertyTree 取值。
    /// </summary>
    public class ContainerSlot
    {
        /// <summary>不可变配置（来自 PropertyTree Struct 节点）。</summary>
        public SlotDef Def { get; }

        /// <summary>当前容纳的实体。</summary>
        public List<Entity> Items { get; } = new();

        /// <summary>槽内物品总重缓存。</summary>
        public float CurrentWeight { get; private set; }

        /// <summary>槽是否已满（且无可堆叠的同类物品）。</summary>
        public bool IsFull => Items.Count >= Def.Capacity;

        /// <summary>槽内第一个可堆叠的同 Preset Entity。不存在返回 null。</summary>
        private Entity StackTarget(Entity incoming)
        {
            if (incoming?.Preset == null || Items.Count == 0) return null;
            foreach (var item in Items)
            {
                if (item.Preset == incoming.Preset && item.CanStack)
                    return item;
            }
            return null;
        }

        public bool IsEmpty => Items.Count == 0;

        public ContainerSlot(SlotDef def)
        {
            Def = def;
        }

        /// <summary>
        /// 检查实体是否可放入此槽位。
        /// 顺序：存在性 → 堆叠合并 → 容量 → AcceptTags → WeightLimit。
        /// </summary>
        public bool CanAccept(Entity entity)
        {
            if (entity == null) return false;

            // 堆叠合并 — 同 Preset + 可堆叠 → 不占新槽位
            var stackTarget = StackTarget(entity);
            if (stackTarget != null)
            {
                // 合并后的重量
                var mergedWeight = CurrentWeight + entity.Properties.GetFloat("Common/Weight");
                if (Def.WeightLimit > 0f && mergedWeight > Def.WeightLimit)
                    return false;
                return true;
            }

            // 新槽位 — 需要容量
            if (IsFull) return false;

            // Tag 过滤 — AcceptTags 非空时须有交集
            if (Def.AcceptTags is { Length: > 0 })
            {
                var itemTags = entity.Properties.GetTagList("Common/ItemTags");
                if (itemTags == null || itemTags.Length == 0) return false;

                bool matched = false;
                foreach (var acceptTag in Def.AcceptTags)
                {
                    if (acceptTag == null) continue;
                    foreach (var itemTag in itemTags)
                    {
                        if (itemTag == acceptTag) { matched = true; break; }
                    }
                    if (matched) break;
                }
                if (!matched) return false;
            }

            // 重量检查 — 0 = 不限
            var weight = entity.Properties.GetFloat("Common/Weight");
            if (Def.WeightLimit > 0f && CurrentWeight + weight > Def.WeightLimit)
                return false;

            return true;
        }

        /// <summary>
        /// 放入实体。同 Preset 可堆叠时合并 StackCount，否则追加新项。
        /// 返回被合并后应销毁的 Entity（调用方负责 Unregister）。
        /// 不合并时返回 null。
        /// </summary>
        public Entity Place(Entity entity)
        {
            if (!CanAccept(entity)) return null;

            var stackTarget = StackTarget(entity);
            if (stackTarget != null)
            {
                int space = stackTarget.MaxStackSize - stackTarget.StackCount;
                int take = System.Math.Min(entity.StackCount, space);
                stackTarget.StackCount += take;

                var weight = entity.Properties.GetFloat("Common/Weight");
                CurrentWeight += weight * take;

                if (take >= entity.StackCount)
                {
                    // 新实体完全被合并 → 应销毁
                    return entity;
                }

                // 新实体部分被合并 → 剩余 back
                entity.StackCount -= take;
                return null;
            }

            // 新槽位
            Items.Add(entity);

            var w = entity.Properties.GetFloat("Common/Weight");
            CurrentWeight += w * entity.StackCount;
            return null;
        }

        /// <summary>
        /// 按引用移除实体。未找到返回 false。
        /// </summary>
        public bool Remove(Entity entity)
        {
            if (!Items.Remove(entity)) return false;
            CurrentWeight -= entity.Properties.GetFloat("Common/Weight") * entity.StackCount;
            return true;
        }

        /// <summary>
        /// 按 EntityId 移除实体。未找到返回 null。
        /// </summary>
        public Entity Remove(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Id == entityId)
                {
                    var entity = Items[i];
                    Items.RemoveAt(i);
                    CurrentWeight -= entity.Properties.GetFloat("Common/Weight") * entity.StackCount;
                    return entity;
                }
            }
            return null;
        }
    }
}
