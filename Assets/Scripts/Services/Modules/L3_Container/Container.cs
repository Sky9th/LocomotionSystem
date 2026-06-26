using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RedDust.Container
{
    /// <summary>
    /// 泛型容器——管理物品的放置、取出、过滤和 Tick。
    ///
    /// 技术文档: .agent/tech/L2-services/L2-modules/L3-container/container.md
    ///
    /// T 取值：
    ///   ItemInstance — 物品容器（身体槽、背包、世界箱子）
    ///   AbilityDefSO — 技能槽（Q/E/R/F 技能栏）
    ///
    /// 容器不负责 Tick——由容器所有者在 Update 中调用 Container.Tick(dt)。
    /// </summary>
    public class Container<T>
    {
        public string ContainerId { get; }

        /// <summary>按 SlotKey 索引的槽位表。</summary>
        public IReadOnlyDictionary<string, ContainerSlot<T>> Slots => _slots;

        /// <summary>有序槽位列表（按构造顺序）。</summary>
        public IReadOnlyList<ContainerSlot<T>> SlotsOrdered { get; }

        /// <summary>所有槽位物品总重。</summary>
        public float CurrentTotalWeight { get; private set; }

        /// <summary>容器承载重量上限。0 = 无限制。</summary>
        public float CarryWeightMax { get; }

        private readonly Dictionary<string, ContainerSlot<T>> _slots = new();
        private readonly List<ContainerSlot<T>> _slotsOrdered = new();

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

                var slot = new ContainerSlot<T>(def);
                _slots[def.SlotId] = slot;
                _slotsOrdered.Add(slot);
            }
        }

        /// <summary>
        /// 检查 item 是否可放入 slotKey 槽位。
        /// </summary>
        public bool CanAccept(string slotKey, T item)
        {
            if (!_slots.TryGetValue(slotKey, out var slot)) return false;
            return slot.CanAccept(item);
        }

        /// <summary>
        /// 放入物品。先调 CanAccept，失败返回 false。
        /// 成功 → 更新 CurrentTotalWeight。
        /// </summary>
        public bool Place(string slotKey, T item)
        {
            if (!_slots.TryGetValue(slotKey, out var slot)) return false;
            if (!slot.Place(item)) return false;
            // TODO ItemInstance 到位后: CurrentTotalWeight += item.Weight
            return true;
        }

        /// <summary>
        /// 按 itemId 从指定槽位移除。未找到返回 default。
        /// </summary>
        public T Remove(string slotKey, string itemId)
        {
            if (!_slots.TryGetValue(slotKey, out var slot)) return default;
            var item = slot.Remove(itemId);
            if (item != null)
            {
                // TODO ItemInstance 到位后: CurrentTotalWeight -= item.Weight
            }
            return item;
        }

        /// <summary>
        /// 按引用移除物品。未找到返回 false。
        /// </summary>
        public bool Remove(string slotKey, T item)
        {
            if (!_slots.TryGetValue(slotKey, out var slot)) return false;
            if (!slot.Remove(item)) return false;
            // TODO ItemInstance 到位后: CurrentTotalWeight -= item.Weight
            return true;
        }

        /// <summary>
        /// 找到第一个能接受该物品的槽位 SlotKey。没有返回 null。
        /// </summary>
        public string FindSlotFor(T item)
        {
            foreach (var slot in _slotsOrdered)
            {
                if (slot.CanAccept(item))
                    return slot.Def.SlotId;
            }
            return null;
        }

        /// <summary>
        /// 所有槽位中所有物品的枚举器。
        /// </summary>
        public IEnumerable<T> AllItems()
        {
            foreach (var slot in _slotsOrdered)
            {
                foreach (var item in slot.Items)
                    yield return item;
            }
        }

        /// <summary>
        /// 获取指定槽位，不存在返回 null。
        /// </summary>
        public ContainerSlot<T> GetSlot(string slotKey)
        {
            _slots.TryGetValue(slotKey, out var slot);
            return slot;
        }

        /// <summary>
        /// 遍历所有槽位的所有物品，逐调 item.Tick(dt)。
        /// 由容器所有者驱动（CharacterActor 60fps / WorldManager 0.5Hz）。
        /// </summary>
        public void Tick(float dt)
        {
            // TODO ItemInstance 到位后: foreach item in AllItems() → item.Tick(dt)
            // 当前 T 无 Tick 方法，空转。
        }
    }
}
