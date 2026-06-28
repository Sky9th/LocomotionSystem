# 2026-06-28 — 嵌套容器 + L4 Equipment 子模块

## Background

PlayerService 测试代码手动 `new Container.Container(...)` 创建背包内部容器，越过整个容器系统。装备同步（GO 挂骨骼 + GripTag 更新）无归属模块。

本 session 将嵌套容器创建收归 EntityService.Register（自动检测 `Slots/` 子节点），新建 L4 CharacterEquipment 临时子模块接管装备同步。三 Agent 多轮审核最终方案。

## Changes

### 嵌套容器归属 EntityService
- Entity +`NestedContainer` 属性
- EntityService.Register → `TryCreateNestedContainer` — 读 `Properties.GetChildren("Slots")` → 建 Container（同步 CharacterContainer.OnWire 模式）
- EntityService.Unregister 级联清理子实体（递归注销 NestedContainer.AllItems）
- Container.Tick(dt, depth=0) 递归传播 + 环检测 (maxDepth=10)
- `ContainerSlot` PropertyDefSO (Struct, Capacity=20) — 通用容器槽位
- Backpack PropertyTree — 继承 ContainerBase，`Slots/ContainerSlot` 节点
- Backpack.asset / ContainerSlot.asset 新建

### L4 CharacterEquipment 临时子模块
- `CharacterEquipment` (ModuleChild) — `SyncEquipment()` 每帧由 CharacterActor 调用：
  - 读 BodyContainer 状态 → diff 快照 → Spawn/Despawn 武器 GO
  - `SyncGripTags` — 从所有装备 Entity 的 `Common/Tags` 提取 `Equip.Grip.*`，写回 `OwnedGripTags`
  - Container 全空时跳过 GripTag 同步，保留 PlayerDirector hack 可用
- `SlotBoneMapper` — 静态 SlotId→HumanBodyBones 映射，`Animator.GetBoneTransform` 抽象
- CharacterActor 3 处改动：字段 + 构造 + Update 中 `equipment.SyncEquipment()` 调用（Kinematic 之后、GripTable.Resolve 之前）

### 数据资产修正
- Blade.asset — `Common/Tags` +`Equip.Grip.1H_Blade`
- Pistol.asset — `Common/Tags` +`Equip.Grip.1H_Sidearm`
- PlayerDirector 移除 `ResolveBodyForm` — BodyForm 恒 Relax（尚未到 Combat 切换阶段）

### PlayerService 测试代码清理
- Blade/Pistol 放入 Backpack 的 NestedContainer，不再直接放手掌槽位
- 删本地 `new Container` / `new SlotDef[]` 创建

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| NestedContainer 放 Entity 上 | A: ContainerResolver 外部字典 → Entity 不感知，但存档/序列化需额外映射。B: 放 ContainerSlot → 只有极少数容器类物品需要，挂在 Entity 上更直接 | Entity 本身已有 View/Tick 等运行时属性，存档时遍历 Entity 即可带走嵌套内容 |
| Container.Tick 递归由 Container 自己处理 | A: Entity.Tick 调 NestedContainer.Tick → 泄漏 depth 参数到 Entity 签名。B: 外部调用方传 depth → 所有 Tick 调用方需改 | Container 内部递归，Entity 不感知 depth |
| 槽位只读 `Slots/` Struct 节点，不认 `SlotCount` int | A: int 模式自动生成 Slot0~Slot9 → 无 AcceptTags 语义，和 Struct 模式分化 | 与技术文档一致，`Capacity` 是槽内物品数量上限而非子槽位数 |
| SlotBoneMapper 硬编码静态字典 | A: ScriptableObject 资产 → 当前只有人形角色，临时方案无需资产化 | Unity Humanoid Avatar 已提供 `GetBoneTransform` 抽象，静态字典够用 |
| `ContainerSlot` 命名 | A: 叫 `Main` → 单槽背包语义不通用。B: 和 defId 同步 → 冗余 nodeId | 当前 nodeId=defId 一致，JSON TODO 标记远期简化 |

## Known Issues

- [ ] NestedContainer 内装备的 GO 不会被 CharacterEquipment 感知 → 武器 prefab 不可见 (P1 — 后续扩展 CharacterEquipment 读 NestedContainer)
- [ ] `LocomotionSet_1H_Blade_Relax` 的 walkMixer/runMixer 为空 → Blade grip 只在 Arm 层叠 idle，无全身移动动画 (P2 — 补动画 clip)
- [ ] Backpack ItemDefSO 资产需在 Unity Editor 中创建并挂 Template=Backpack

## Cross-References

### Related Sessions
- [2026-06-28-slots-propertytree-entity-pipeline.md](2026-06-28-slots-propertytree-entity-pipeline.md) — Slots PropertyTree 改造，CharacterContainer 建立 BodyContainer
- [2026-06-27-container-character-slots.md](2026-06-27-container-character-slots.md) — Container 系统落地 + CharacterContainer 初版
- [2026-06-24-equipment-item-architecture.md](2026-06-24-equipment-item-architecture.md) — 装备/物品架构设计，容器嵌套与 MergeSlotsFrom

### Related Plans
- [../plans/recursive-watching-frog.md](../plans/recursive-watching-frog.md) — 本 session 实施计划

### Related Tech Docs (待更新)
- tech/L2-services/L2-entity-service/ — Entity + NestedContainer, EntityService.TryCreateNestedContainer, Unregister cascade
- tech/L2-services/L2-modules/L3-container/ — Container.Tick depth, ContainerSlot Capacity 语义
- tech/L2-services/L2-modules/L3-character/ — L4_Equipment, CharacterEquipment, SlotBoneMapper
- tech/L2-services/L2-modules/L3-properties/ — ContainerSlot PropertyDefSO

### Flag for Design Doc Creation
- [x] No design doc needed — internal infrastructure, no player-facing behavior change.
