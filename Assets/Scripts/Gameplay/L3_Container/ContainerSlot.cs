using RedDust.Gameplay.Container;
using System.Collections.Generic;
using RedDust.Services.EntityService;
using UnityEngine;

namespace RedDust.Gameplay.Container
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

        private static float WeightOf(Entity entity) => entity.Properties.GetFloat("Common/Weight");

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

            // 重量 — 堆叠和新物品都要过
            if (Def.WeightLimit > 0f && CurrentWeight + WeightOf(entity) > Def.WeightLimit)
                return false;

            // 堆叠 — 同 Preset + 可堆叠 → 不占新槽位，跳过 Tag/Capacity
            if (StackTarget(entity) != null) return true;

            return HasCapacityFor(entity) && AcceptsTag(entity);
        }

        /// <summary>检查 incoming 是否可替换 outgoing。跳过容量检查，只校验 Tag + 置换后重量。</summary>
        public bool CanSwap(Entity incoming, Entity outgoing)
        {
            if (incoming == null) return false;
            if (!AcceptsTag(incoming)) return false;

            float outgoingWeight = outgoing?.Properties.GetFloat("Common/Weight") ?? 0f;
            float incomingWeight = incoming.Properties.GetFloat("Common/Weight");
            float newWeight = CurrentWeight - outgoingWeight + incomingWeight;
            if (Def.WeightLimit > 0f && newWeight > Def.WeightLimit)
                return false;

            return true;
        }

        private bool AcceptsTag(Entity entity)
        {
            if (Def.AcceptTags is not { Length: > 0 }) return true;
            var tags = entity.Properties.GetTagList(Entity.CommonTagsPath);
            if (tags == null || tags.Length == 0) return false;
            foreach (var acceptTag in Def.AcceptTags)
            {
                if (acceptTag == null) continue;
                foreach (var tag in tags)
                    if (tag == acceptTag || tag.StartsWith(acceptTag + ".")) return true;
            }
            return false;
        }

        private bool HasCapacityFor(Entity entity)
        {
            return StackTarget(entity) != null || !IsFull;
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

                var weight = WeightOf(entity);
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

            var w = WeightOf(entity);
            CurrentWeight += w * entity.StackCount;
            return null;
        }

        /// <summary>
        /// 按引用移除实体。未找到返回 false。
        /// </summary>
        public bool Remove(Entity entity)
        {
            if (!Items.Remove(entity)) return false;
            CurrentWeight -= WeightOf(entity) * entity.StackCount;
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
                    Remove(entity);
                    return entity;
                }
            }
            return null;
        }
    }
}
