using RedDust.Properties;
using UnityEngine;

namespace RedDust.Entities
{
    /// <summary>
    /// Entity 的只读查询门面——始终可用，无论 Entity 是否有 spawned GO。
    ///
    /// 分层设计：
    ///   L0 Identity  — Id / Preset / StackCount / HasView（始终可用）
    ///   L1 Vitals    — HP / Hunger / IsAlive（读 PropertyTable，始终可用）
    ///   L2 Inventory — 容器物品（包装 NestedContainer，始终可用）
    ///   L3 Equipment — 装备槽位（包装 BodyContainer，角色 spawn 后可用）
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
        public Container.Container NestedContainer => _entity.NestedContainer;
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
        /// 容器物品查询。包装 _entity.NestedContainer，始终可用（无容器时为 null）。
        /// 用法：entity.Query.Inventory.AllItems / .FindItem("hp_potion") / .HasItem(...)
        /// </summary>
        public InventoryQuery Inventory { get; }

        // ═══════════════════════════════════════════════════════════════
        // L3: Equipment —— 身体装备槽位（包装 BodyContainer）
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 身体装备槽位查询。CharacterActor spawn 后设置（包装 BodyContainer）。
        /// 非角色 Entity 或未 spawn 时为 null。
        /// 用法：entity.Query.Equipment?.GetEquipped("RightHand") / .RightHand
        /// </summary>
        public EquipmentQuery Equipment { get; internal set; }

        // ═══════════════════════════════════════════════════════════════
        // L5: State —— 运行时状态（Entity 自己就能回答）
        // ═══════════════════════════════════════════════════════════════

        /// <summary>最后已知世界位置。有 View 时从 Transform 读，无 GO 时为 null。</summary>
        public Vector3? LastKnownPosition => _entity.View != null ? _entity.View.transform.position : null;

        // Future: IsMoving / Velocity — 当下游消费者明确需要时，
        // 可通过 LastMotor/LastDiscrete 等已缓存的离散状态推算，不需要 Actor 暴露额外属性。

        // ── ctor ──────────────────────────────────────────────────────

        internal EntityQueryModule(Entity entity)
        {
            _entity = entity;
            Vitals = new VitalsQuery(entity.Properties);
            Inventory = new InventoryQuery(entity.NestedContainer);
            // Equipment — MVP 为 null，后续由 CharacterActor 或 BodyContainer 系统设置
            // State — LastKnownPosition 直接从 View.transform 读，无需写入
        }
    }
}
