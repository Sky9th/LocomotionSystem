using RedDust.Gameplay.Container;
using System.Collections.Generic;
using System.Linq;
using RedDust.Gameplay.Character;

namespace RedDust.Services.EntityService
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
        private readonly RdContainer _bodyContainer;

        /// <summary>获取指定槽位的装备（如 "RightHand", "Head"）</summary>
        public Entity GetEquipped(string slotId)
        {
            return _bodyContainer?.GetItem(slotId);
        }

        /// <summary>指定槽位是否有装备</summary>
        public bool IsSlotOccupied(string slotId)
        {
            return _bodyContainer?.GetItem(slotId) != null;
        }

        /// <summary>右手武器（便捷属性）</summary>
        public Entity RightHand => GetEquipped("RightHand");

        internal EquipmentQuery(RdContainer bodyContainer)
        {
            _bodyContainer = bodyContainer;
        }
    }
}
