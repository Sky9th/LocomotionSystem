using System.Collections.Generic;
using System.Linq;
using RedDust.Character;

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
        private readonly Container.Container _container;

        /// <summary>容器中的所有物品</summary>
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

        internal InventoryQuery(Container.Container container)
        {
            _container = container;
        }
    }
}
