using System.Collections.Generic;
using RedDust.Entities;
using UnityEngine;

namespace RedDust.Container
{
    /// <summary>
    /// 实体容器——管理 Entity 的放置、取出、过滤和 Tick。
    ///
    /// 技术文档: .agent/tech/L2-services/L2-modules/L3-container/container.md
    ///
    /// 容器不负责 Tick——由容器所有者在 Update 中调用 Container.Tick(dt)。
    /// Entity 引用指向 EntityService 注册表中的同一对象。
    /// </summary>
    public class Container
    {
        public string ContainerId { get; }

        /// <summary>按 SlotKey 索引的槽位表。</summary>
        public IReadOnlyDictionary<string, ContainerSlot> Slots => _slots;

        /// <summary>有序槽位列表（按构造顺序）。</summary>
        public IReadOnlyList<ContainerSlot> SlotsOrdered { get; }

        /// <summary>所有槽位物品总重。</summary>
        public float CurrentTotalWeight { get; private set; }

        /// <summary>容器承载重量上限。0 = 无限制。</summary>
        public float CarryWeightMax { get; }

        private readonly Dictionary<string, ContainerSlot> _slots = new();
        private readonly List<ContainerSlot> _slotsOrdered = new();

        /// <summary>
        /// 从 SlotDef[] 构造容器。
        /// SlotId 重复 → Debug.LogError + skip，不抛异常。
        /// </summary>
        public Container(string containerId, SlotDef[] slotDefs, float carryWeightMax = 0f)
        {
            ContainerId = containerId;
            CarryWeightMax = carryWeightMax;

            if (slotDefs == null || slotDefs.Length == 0) return;

            foreach (var def in slotDefs)
            {
                if (string.IsNullOrEmpty(def.SlotId))
                {
                    Debug.LogError($"[Container] {containerId}: SlotDef has empty SlotId, skipped.");
                    continue;
                }

                if (_slots.ContainsKey(def.SlotId))
                {
                    Debug.LogError($"[Container] {containerId}: Duplicate SlotId '{def.SlotId}', skipped.");
                    continue;
                }

                var slot = new ContainerSlot(def);
                _slots[def.SlotId] = slot;
                _slotsOrdered.Add(slot);
            }
        }

        /// <summary>
        /// 检查实体是否可放入 slotKey 槽位。
        /// </summary>
        public bool CanAccept(string slotKey, Entity entity)
        {
            if (!_slots.TryGetValue(slotKey, out var slot)) return false;
            return slot.CanAccept(entity);
        }

        /// <summary>
        /// 放入实体。可堆叠时合并 StackCount。
        /// 返回被完全合并应销毁的 Entity（null = 已放置，无需额外处理）。
        /// </summary>
        public Entity Place(string slotKey, Entity entity)
        {
            if (!_slots.TryGetValue(slotKey, out var slot)) return null;

            var weightBefore = slot.CurrentWeight;
            var consumed = slot.Place(entity);
            CurrentTotalWeight += slot.CurrentWeight - weightBefore;

            return consumed;
        }

        /// <summary>
        /// 按 EntityId 从指定槽位移除。未找到返回 null。
        /// </summary>
        public Entity Remove(string slotKey, string entityId)
        {
            if (!_slots.TryGetValue(slotKey, out var slot)) return null;
            var entity = slot.Remove(entityId);
            if (entity != null)
                CurrentTotalWeight -= entity.Properties.GetFloat("Common/Weight") * entity.StackCount;
            return entity;
        }

        /// <summary>
        /// 按引用移除实体。未找到返回 false。
        /// </summary>
        public bool Remove(string slotKey, Entity entity)
        {
            if (!_slots.TryGetValue(slotKey, out var slot)) return false;
            if (!slot.Remove(entity)) return false;
            CurrentTotalWeight -= entity.Properties.GetFloat("Common/Weight") * entity.StackCount;
            return true;
        }

        /// <summary>
        /// 找到第一个能接受该实体的槽位 SlotKey。没有返回 null。
        /// </summary>
        public string FindSlotFor(Entity entity)
        {
            foreach (var slot in _slotsOrdered)
            {
                if (slot.CanAccept(entity))
                    return slot.Def.SlotId;
            }
            return null;
        }

        /// <summary>
        /// 所有槽位中所有实体的枚举器。
        /// </summary>
        public IEnumerable<Entity> AllItems()
        {
            foreach (var slot in _slotsOrdered)
            {
                foreach (var entity in slot.Items)
                    yield return entity;
            }
        }

        /// <summary>
        /// 获取指定槽位，不存在返回 null。
        /// </summary>
        public ContainerSlot GetSlot(string slotKey)
        {
            _slots.TryGetValue(slotKey, out var slot);
            return slot;
        }

        /// <summary>
        /// 遍历所有槽位的所有实体，逐调 entity.Tick(dt)。
        /// 由容器所有者驱动。
        /// </summary>
        public void Tick(float dt)
        {
            foreach (var entity in AllItems())
                entity.Tick(dt);
        }
    }
}
