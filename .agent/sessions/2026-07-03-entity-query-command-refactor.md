# 2026-07-03 — Entity.Command/Query 全量落地

## Background

之前的 UI 和 PlayerService 通过 `_playerActor.BuildContext` 四层穿透直达 L3 Character 内部（CharacterContainer.BodyContainer、AbilityExecutor、AbilityForest）。Entity 已有 Command/Query 体系框架但未完整接线——EquipmentQuery 的 GetAllEquipped 空实现、InventoryQuery 构造函数缓存 null NestedContainer、AbilityQuery 不存在。

本次会话目标：将 Entity.Query/Command 打造为唯一外部访问入口，消除所有 Actor 穿透和 Container 引用暴露。

## Changes

### Entity.Query 层（L2）
- **新增 AbilityQuery** — 惰性包装 AbilityExecutor + AbilityForest，暴露 ActiveAbilities / GetCooldownRemaining / IsActive
- **EntityQueryModule** — Equipment / Inventory / Ability 全部改为惰性 getter，从 Entity 自身数据自解析，无需外部接线
- **EquipmentQuery** — GetAllEquipped 实现，遍历 SlotsOrdered 返回非空槽位
- **InventoryQuery** — 构造函数缓存 null NestedContainer 的 bug 修复

### Entity.Command 层（L2）
- **+Place / +Remove** — 外部系统通过 Command 操作物品，不持 Container 引用
- **CycleEquip** — 改用 Query.Equipment 读装备 + Command.Place/Remove 写操作，消内部 Container 引用
- **UseActiveAbility** — weapon 来源从 `actor.Container.BodyContainer` 改为 `Query.Equipment.GetEquipped`

### 删除 CharacterContainer（L3）
- 身体槽位统一走 `entity.NestedContainer`（EntityService.Register 时从 Properties→Slots 创建）
- CharacterBuildContext +Container 属性，CharacterActor.Start 赋值 `identity.Entity.NestedContainer`
- CharacterEquipment 改用 `ctx.Container`

### 全局去 Actor 穿透
- **PlayerService** — 删除 `_playerActor`，全部改用 `_playerEntity.Command/Query`
- **UIService** — 删除 `PlayerActor` 属性，HandlePlayerSpawned 通过 entityId lookup Entity
- **AbilityBarOverlay** — Entity.Query.Ability
- **WeaponBarOverlay** — Entity.Query.Equipment.GetAllEquipped
- **DebugOverlay** — Entity.Query.Ability

### 其他
- **SPlayerSpawnedEvent** — L1 解耦：Entity → entityId
- **Identity** — +Entity 引用（纯数据互引用）
- **Entity.CommonTagsPath** — 消除 `"Common/Tags"` 魔术字符串
- **Container→RdContainer** — 重命名

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Query 惰性自解析，不外部接线 | A: PlayerService 在 yield return null 后接线 → 增加外部系统负担。B: 单独 WireQuery 方法 → L2→L3 单向但耦合 | Entity 自己有 View/Properties/NestedContainer，完全能自解析 |
| Entity.NestedContainer 即身体容器 | A: 新建 BodyContainer 属性 → 冗余。B: 改名→影响面大 | 角色 Entity 的 NestedContainer = 身体槽位，背包 Entity 的 NestedContainer = 存储槽位，同结构不同语义 |
| Command.Place/Remove 作为容器操作入口 | A: 单独 Equip/Store 方法 → 实现完全相同。B: 公开 Container 引用 → 外部直接操作 | 一个 Place 方法，slotKey 区分语义 |
| 保留 Identity.Entity 引用 | A: 审核建议删除（循环引用）→ CharacterEquipment 需另找路径读 Container | Entity↔Identity 是数据对象互引用，非服务依赖 |
| Container→RdContainer 重命名 | A: 保持 Container.Container → 命名混淆（类名=命名空间名） | RdContainer 消除歧义 |
| SPlayerSpawnedEvent Entity→entityId | A: 保留 Entity → L1 依赖 L2 类型 | entityId 足够，消费者自行 lookup |

## Known Issues

- [ ] EntityQueryModule/EntityCommandModule (L2) 引用 CharacterActor (L3) — 必要桥接，设计张力可接受 (P2)
- [ ] DebugOverlay 删除了 stateTimeLabel 序列化字段 — 现有 prefab 可能报警告 (P2)
- [ ] PlayerService.SpawnTestEntities 仍为测试代码，远期需迁移到正式装备系统 (P2)
- [ ] CycleEquip 仍用硬编码 EquipMap，远期需装备系统替代 (P2)

## Cross-References

### Related Plans
- [../plans/staged-bouncing-squirrel.md](../plans/staged-bouncing-squirrel.md) — 本会话执行计划

### Related Tech Docs
- [tech/L2-services/L2-ui/ui-service.md](../tech/L2-services/L2-ui/ui-service.md) — UIService API 变更
- [tech/L2-services/L2-entity-service/](../tech/L2-services/L2-entity-service/) — Entity.Query/Command 新增

### Flag for Design Doc Creation
- [x] No design doc needed — internal architecture refactor, no player-facing behavior changes.
