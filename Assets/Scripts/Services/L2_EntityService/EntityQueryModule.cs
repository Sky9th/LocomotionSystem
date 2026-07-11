using RedDust.Gameplay.Container;
using RedDust.Gameplay.Character;
using RedDust.Gameplay.Properties;
using UnityEngine;

namespace RedDust.Services.EntityService
{
    /// <summary>
    /// Entity 的只读查询门面——始终可用，无论 Entity 是否有 spawned GO。
    /// 所有查询惰性自解析，无需外部接线。
    ///
    /// 分层设计：
    ///   L0 Identity  — Id / Preset / StackCount / HasView（始终可用）
    ///   L1 Vitals    — HP / Hunger / IsAlive（读 PropertyTable，始终可用）
    ///   L2 Inventory — 容器物品（包装 NestedContainer，始终可用）
    ///   L3 Equipment — 装备槽位（包装 NestedContainer，角色 spawn 后可用）
    ///   L4 Ability   — 技能查询（惰性从 View→Actor 解析，角色 spawn 后可用）
    ///   L5 State     — LastKnownPosition（从 View.transform 读，无 GO 时为 null）
    ///
    /// 无接口——只有一个实现者时不抽象。
    /// </summary>
    public class EntityQueryModule
    {
        private readonly Entity _entity;

        // ═══════════════════════════════════════════════════════════════
        // L0: Identity —— 始终可用
        // ═══════════════════════════════════════════════════════════════

        public string Id => _entity.Id;
        public PropertyPresetSO Preset => _entity.Preset;
        public PropertyTable Properties => _entity.Properties;
        public int StackCount => _entity.StackCount;
        public int MaxStackSize => _entity.MaxStackSize;
        public bool CanStack => _entity.CanStack;
        public RdContainer NestedContainer => _entity.NestedContainer;
        public bool HasView => _entity.HasView;
        public GameObject View => _entity.View;

        // ═══════════════════════════════════════════════════════════════
        // L1: Vitals —— 生理特征（始终可用，读 PropertyTable）
        // ═══════════════════════════════════════════════════════════════

        public readonly VitalsQuery Vitals;

        // ═══════════════════════════════════════════════════════════════
        // L2: Inventory —— 容器物品查询（包装 NestedContainer）
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 容器物品查询。惰性包装 _entity.NestedContainer（Register 后可用）。
        /// </summary>
        public InventoryQuery Inventory
        {
            get
            {
                if (_inventory == null && _entity.NestedContainer != null)
                    _inventory = new InventoryQuery(_entity.NestedContainer);
                return _inventory;
            }
        }

        private InventoryQuery _inventory;

        // ═══════════════════════════════════════════════════════════════
        // L3: Equipment —— 身体装备槽位（包装 NestedContainer）
        // ═══════════════════════════════════════════════════════════════

        private EquipmentQuery _equipment;

        /// <summary>
        /// 身体装备槽位查询。惰性包装 entity.NestedContainer（纯数据，无 GO）。
        /// EntityService.Register 时已从 Properties→Slots 创建。
        /// 用法：entity.Query.Equipment?.GetAllEquipped() / .GetEquipped("RightHand")
        /// </summary>
        public EquipmentQuery Equipment
        {
            get
            {
                if (_equipment == null && _entity.NestedContainer != null)
                    _equipment = new EquipmentQuery(_entity.NestedContainer);
                return _equipment;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // L4: Ability —— 技能查询（惰性从 View→Actor 解析）
        // ═══════════════════════════════════════════════════════════════

        private AbilityQuery _ability;

        /// <summary>
        /// 技能查询。惰性从 entity.View→CharacterActor 解析。
        /// 非角色 Entity / 无 GO / 组件未就绪时为 null。
        /// 用法：entity.Query.Ability?.ActiveAbilities / .GetCooldownRemaining(a) / .IsActive(a)
        /// </summary>
        public AbilityQuery Ability
        {
            get
            {
                if (_ability == null)
                {
                    // Use Unity's overloaded != null — C# ?. bypasses fake-null detection
                    if (_entity.View != null)
                    {
                        var actor = _entity.View.GetComponent<CharacterActor>();
                        if (actor != null)
                        {
                            var ctx = actor.BuildContext;
                            if (ctx != null)
                                _ability = new AbilityQuery(ctx.Ability, ctx.AbilityForest);
                        }
                    }
                }
                return _ability;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // L5: State —— 运行时状态（Entity 自己就能回答）
        // ═══════════════════════════════════════════════════════════════

        /// <summary>最后已知世界位置。有 View 时从 Transform 读，无 GO 时为 null。</summary>
        public Vector3? LastKnownPosition => _entity.View != null ? _entity.View.transform.position : null;

        // ── ctor ──────────────────────────────────────────────────────

        internal EntityQueryModule(Entity entity)
        {
            _entity = entity;
            Vitals = new VitalsQuery(entity.Properties);
        }
    }
}
