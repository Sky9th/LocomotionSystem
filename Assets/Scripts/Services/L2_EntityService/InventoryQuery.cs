using System.Collections.Generic;
using System.Linq;
using RedDust.Character;
using UnityEngine;

namespace RedDust.Entities
{
    /// <summary>
    /// 容器物品查询（L2）—— 具体类, 无接口。
    ///
    /// 包装一个 Container.Container，始终可用。
    /// EntityQueryModule.Inventory 包装 _entity.NestedContainer（箱子/背包的内容物）。
    /// 无容器时为 null。
    ///
    /// Container.Container 是 L3 类型，但此类只是数据门面——不做写操作。
    /// </summary>
    public class InventoryQuery
    {
        private readonly Container.RdContainer _container;

        // ═══════════════════════════════════════════════════════════════
        // 深层递归缓存 —— key = "{entityId}/{slotPath}"
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 全槽位物品缓存（含嵌套容器）。
        /// key = "{entityId}/{slotId1}/{slotId2}/..."，value = Entity。
        /// 例如：背包中的弹药 → "ammo_01/ContainerSlot/Ammo"
        /// 调用 RefreshAllItemsDeep() 重建。
        /// </summary>
        private Dictionary<string, Entity> _allItemsDeepCache;

        /// <summary>递归收集所有嵌套容器中的物品，缓存尚未构建时惰性重建。</summary>
        public IReadOnlyDictionary<string, Entity> AllItemsDeep
        {
            get
            {
                if (_allItemsDeepCache == null)
                    RefreshAllItemsDeep();
                return _allItemsDeepCache;
            }
        }

        /// <summary>
        /// 重建全槽位物品缓存。物品变动后调用。
        /// 从根容器出发，递归深入每个物品的 NestedContainer，最大深度 10。
        /// </summary>
        public void RefreshAllItemsDeep()
        {
            _allItemsDeepCache = new Dictionary<string, Entity>();
            if (_container == null) return;
            CollectRecursive(_container, "", _allItemsDeepCache, 0);
        }

        /// <summary>按 entityId 查找嵌套物品（搜索所有层级，含深层容器）。O(n)。</summary>
        public Entity FindItemDeep(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            foreach (var entity in AllItemsDeep.Values)
            {
                if (entity.Id == entityId) return entity;
            }
            return null;
        }

        /// <summary>
        /// 递归收集：遍历 container 所有槽位，将物品写入 cache，然后深入物品的 NestedContainer。
        /// </summary>
        private static void CollectRecursive(
            Container.RdContainer container,
            string parentPath,
            Dictionary<string, Entity> cache,
            int depth)
        {
            const int maxDepth = 10;
            if (depth >= maxDepth)
            {
                Debug.LogError($"[InventoryQuery] max depth {maxDepth} exceeded at '{parentPath}' — possible container cycle.");
                return;
            }

            foreach (var slot in container.SlotsOrdered)
            {
                if (slot.Items == null) continue;
                foreach (var entity in slot.Items)
                {
                    string path = string.IsNullOrEmpty(parentPath)
                        ? slot.Def.SlotId
                        : $"{parentPath}/{slot.Def.SlotId}";
                    string key = $"{entity.Id}/{path}";
                    cache[key] = entity;

                    if (entity.NestedContainer != null)
                    {
                        CollectRecursive(entity.NestedContainer, path, cache, depth + 1);
                    }
                }
            }
        }

        /// <summary>容器中的所有物品（仅当前层级，不含嵌套容器内的物品）</summary>
        public IReadOnlyList<Entity> AllItems =>
            _container?.AllItems()?.ToList() ?? new List<Entity>();

        /// <summary>按 EntityId 查找（搜索所有槽位）</summary>
        public Entity FindItem(string entityId)
        {
            if (_container == null || string.IsNullOrEmpty(entityId)) return null;
            return _container.FindItem(CharacterConst.Slot.ContainerSlot, entityId)
                ?? _container.AllItems().FirstOrDefault(e => e.Id == entityId);
        }

        /// <summary>是否包含指定物品</summary>
        public bool HasItem(string entityId) => FindItem(entityId) != null;

        /// <summary>统计物品数量（按 EntityId，考虑堆叠 StackCount）</summary>
        public int CountItem(string entityId)
        {
            if (_container == null) return 0;
            return _container.AllItems()
                .Where(e => e.Id == entityId)
                .Sum(e => e.StackCount);
        }

        /// <summary>容器中物品总数（去重前）</summary>
        public int ItemCount => _container?.AllItems()?.Count() ?? 0;

        internal InventoryQuery(Container.RdContainer container)
        {
            _container = container;
        }
    }
}
