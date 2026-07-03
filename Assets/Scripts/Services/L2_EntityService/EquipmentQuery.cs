using System.Collections.Generic;
using System.Linq;

namespace RedDust.Entities
{
    /// <summary>
    /// 身体装备槽位查询（L3）—— 具体类, 无接口。
    ///
    /// 包装角色的 BodyContainer（CharacterContainer 创建的装备槽容器）。
    /// CharacterActor spawn 时通过 Query.Equipment 设置。
    /// 非角色 Entity 为 null。
    /// </summary>
    public class EquipmentQuery
    {
        private readonly Container.Container _bodyContainer;

        /// <summary>获取指定槽位的装备（如 "RightHand", "Head"）</summary>
        public Entity GetEquipped(string slotId)
        {
            return _bodyContainer?.GetItem(slotId);
        }

        /// <summary>获取所有已装备的 (槽位, 物品) 列表</summary>
        public IReadOnlyList<(string slotId, Entity item)> GetAllEquipped()
        {
            // BodyContainer 的 slot 遍历方式取决于 Container 实现。
            // MVP：外部调用者已知槽位名，逐槽查询。
            // Future：Container 暴露 Slots 后可改为自动遍历。
            return new List<(string, Entity)>();
        }

        /// <summary>指定槽位是否有装备</summary>
        public bool IsSlotOccupied(string slotId)
        {
            return _bodyContainer?.GetItem(slotId) != null;
        }

        /// <summary>右手武器（便捷属性）</summary>
        public Entity RightHand => GetEquipped("RightHand");

        internal EquipmentQuery(Container.Container bodyContainer)
        {
            _bodyContainer = bodyContainer;
        }
    }
}
