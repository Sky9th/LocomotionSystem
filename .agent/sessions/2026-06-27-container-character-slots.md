# 2026-06-27 — L3_Container 模块 + 角色身体槽位

## Background

Entity 基树 `Common/Slots`（Struct, SlotDef[]）已就位，`ItemDefSO` 仍持有 `SlotDef[] Slots` C# 字段——违反"一切皆属性"设计原则。运行时无 Container 类，`PlayerDirector.ProcessEquipInput()` 硬编码 3 个槽位直写 `OwnedGripTags`，是过渡方案。

本 session 落地 L3_Container 模块（4 文件）、迁移 SlotDef、完成 ItemDefSO 零字段、建立角色身体槽位管线。属于 S2 装备闭环的前置基础设施。

## Changes

### L3_Container 模块（新建）
- `SlotDef.cs` — 槽位定义 struct，namespace `RedDust.Container`，`[PropertyStruct]` 标记
- `ContainerSlot.cs` — 泛型运行时槽位 `ContainerSlot<T>`，CanAccept/Place/Remove
- `Container.cs` — 泛型容器 `Container<T>`，完整 API：Place/Remove/CanAccept/FindSlotFor/AllItems/Tick/GetSlot
- `ContainerSlotRef.cs` — 轻量定位符 struct（联机兼容），`OwnerId` + `SlotKey`

### SlotDef 搬家 + ItemDefSO 清理
- SlotDef 从 `L3_Item/ItemDefSO.cs` → `L3_Container/SlotDef.cs`（技术文档规定位置）
- ItemDefSO 移除 `SlotDef[] Slots` C# 字段、`OnValidate()` 方法——零字段落地
- `properties_all.json` `structTypeName` → `RedDust.Container.SlotDef`
- `Slots.asset` StructTypeName 同步更新
- `PropertyDefSO.cs` Tooltip 示例 namespace 更新

### 角色身体槽位
- `CharacterContainer`（`L3_Character/Container/`）— ModuleChild，OnWire 从 PropertyTree 读 `Common/Slots`
- `CharacterDefSO` — `StandardBodySlots` 静态定义 + `ISerializationCallbackReceiver.OnBeforeSerialize` 自动注入 OverridesJson
- `CharacterActor` — `internal CharacterContainer Container` 属性，Awake 中 `new CharacterContainer(buildCtx, Registry)`

### 生命周期修正
- BodySlots 读取从构造移至 OnWire——避免 Unity Awake 跨组件调用顺序竞态。`PropertyAgent.Awake()` 可能在 `CharacterActor.Awake()` 之后才执行。

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| SlotDef → L3_Container（非 L3_Properties） | A: L3_Properties（PropertyStruct 归属）→ Properties 是存储层。B: L3_Item（原位置）→ Item 不是唯一消费方 | Container 是 SlotDef 的主要运行时消费者，技术文档已规定此位置 |
| ContainerSlot 非泛型 | A: `ContainerSlot<T>` → 编译独立但有冗余 | 技术文档规定 T 由外层 Container<T> 提供。但 C# 约束迫使采用 `ContainerSlot<T>`——非泛型类无法持有 `List<T>` |
| 身体槽位无代码 fallback | A: 空 Common/Slots → hardcode 9 槽位。B: 读 CharacterDefSO.StandardBodySlots | 非人形角色不该有人形槽位。空 = 无槽位是正确的默认行为 |
| OnWire 读 Agent（非构造/OnAssemble） | A: 构造中读 → NPC 刚好撞对，Player 失败 | Unity Awake 不保证跨组件顺序。OnWire（Start 阶段）保证所有 Awake 已完成 |
| OnBeforeSerialize 注入默认 OverridesJson | A: Reset() → ScriptableObject 不触发。B: OnValidate() → 需刷新才可见。C: OnEnable() → 每次加载都触发 | OnBeforeSerialize 在序列化前必然调用，新建资产即刻有值 |
| 不建 ItemInstance | — | 用户指定不在本次范围 |

## Known Issues

- [ ] Container<T> 无法实际装物品——ItemInstance 未到位。当前 CanAccept 仅做 Capacity 检查，Tag/Weight 过滤标记 TODO
- [ ] ContainerSlotRef 已定义但未接入 L2_ItemService
- [ ] Container.Tick 空转——ItemInstance 无 Tick 方法
- [ ] 武器装备后的 GripTag 事件未接入（PlayerDirector hack 保留）
- [ ] `ContainerSlot<T>.Remove(string itemId)` 未实现——ItemInstance 到位后按 Id 匹配
- [ ] Entity Editor 将接管 CharacterDefSO.OnBeforeSerialize 的批量注入逻辑

## Cross-References

### Related Sessions
- [2026-06-26-entity-base-tree.md](2026-06-26-entity-base-tree.md) — 同日 Entity 基树，Common/Slots 的上游依赖
- [2026-06-26-property-struct-and-rename.md](2026-06-26-property-struct-and-rename.md) — 同日 PropertyType.Struct，SlotDef 能进 PropertyTree 的前提

### Related Plans
- [../plans/short-term-plan.md](../plans/short-term-plan.md) — S2 装备→技能闭环
- [sorted-growing-tarjan.md](../../../sorted-growing-tarjan.md) — 本 session 的实施计划

### Related Tech Docs (待创建/更新)
- [tech/L2-services/L2-modules/L3-container/](../tech/L2-services/L2-modules/L3-container/) — L3_Container 模块文档（新建）
- [tech/L2-services/L2-modules/L3-item/item-def-so.md](../tech/L2-services/L2-modules/L3-item/item-def-so.md) — ItemDefSO 零字段更新
- [tech/L2-services/L2-modules/L3-character/](../tech/L2-services/L2-modules/L3-character/) — CharacterContainer + CharacterDefSO 新增

### Flag for Design Doc Creation
- [x] No design doc needed — internal infrastructure, no player-facing behavior change.
