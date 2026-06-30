# 2026-06-30 — Ability Pipeline 状态机框架

## Background

Property 重构刚完成，MeleeWeaponSO 路径未同步（`Combat/ATK` → `Weapon/ATK`）。接着启动 Ability Pipeline 主动技能管道——8 维度管道中 ②③④⑤ 需从 AbilityExecutor 剥离为独立步骤，用状态机驱动多帧释放流程。同时 Container AcceptTags 精确匹配导致装备切换失败，需改为层级匹配。

## Changes

### Properties 对齐
- `MeleeWeaponSO.cs` + `ItemDefSO.cs` — 路径 `Combat/ATK` → `Weapon/ATK`
- 9 个 StructDef .asset + `properties_all.json` — AcceptTags 加 `Entity.` 前缀

### Ability Pipeline 状态机
- 新建 `Executor/State/` — `IState<TContext>`, `StateMachine<TContext>`, `AbilityState`（泛型零领域依赖，[MARK] 可提至 Shared/）
- 新建 `GatingState.cs` — ② 冷却/互斥/外部条件三闸门
- 新建 `TerminalStates.cs` — `IdleState`, `RejectedState`, `CompletedState`
- 新建 `ActiveAbilityPipeline.cs` — 持有 `StateMachine<SActiveAbilityContext>`, `Start()` → `Tick()` → `Interrupt()`
- 新建 `Enum/ActiveAbilityState.cs` — `Idle → CanEnter → BeforeExe → Execute → AfterExe → CanExit → Completed → Rejected`
- 新建 `Structs/SActiveAbilityContext.cs` — 管道上下文（`Ability`, `Executor`, `WeaponEntity`, `Origin`, `Direction`, `Targets`, `Hits`）

### AbilityExecutor 重构
- 旧代码归档 `#region OLD_IMPLEMENTATION`
- 新增 `Queue<SQueuedSkill>` + `Enqueue()` 对外接口
- 新 `Update` 驱动 `_pipeline.Tick` + 队列消费
- Bugfix: `CleanupExpiredCooldowns()` 遗漏补回

### Container AcceptTags
- `ContainerSlot.AcceptsTag` — 精确匹配改为层级匹配（`==` || `StartsWith(acceptTag + ".")`）
- 9 个身体槽位 StructDef AcceptTags 统一加 `Entity.` 前缀

### State 链自组装
- `GatingState` / `CostState` — 去掉构造参数 `next`/`rejected`，State 内部 `new` 下一站
- `ActiveAbilityPipeline` — 移除 State 组装代码，仅 `new GatingState()`
- `EActiveAbilityState` 枚举值对齐 State 名：`CanEnter→Gating` / `BeforeExe→Cost` / `Execute→Execution` / `AfterExe→Cooldown` / `CanExit→Recovery`

### PlayerDirector 对接
- `TryActivateSkill` — `TryActivate` → `Enqueue`，武器 Entity 从 `BodyContainer.GetItem("RightHand")` 传入

### 计划
- 新建 `plans/log-format-standardization.md` — 日志格式规范化计划（管道完成后执行）
- 更新 `long-term.md` / `short-term.md` — S3 进度 + 施工历史 + 4.1a 日志任务

### 资产
- 新建 `Assets/Data/Entities/Equipment/Backpack.asset`
- 删除 `AbilitySearchUtility.cs`, `AbilityGating.cs`, `AbilityCost.cs`（逻辑迁入 State 类）

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| `IState<TContext>` 泛型接口 | A: 非泛型，硬绑定 SActiveAbilityContext — 不可复用。 B: 直接用抽象类 — 失去接口灵活性。 | 泛型零成本（IL2CPP 编译期特化），为其他模块预留空间。加 [MARK] 标注可提出。 |
| `OnTick` 返回自身=留在当前 | A: 返回 bool + 事件驱动 → 需要额外状态管理。 B: State 持有 StateMachine 引用 → 循环依赖。 | 返回自身是最轻量的"停留"语义，StateMachine 只做 `next != current` 比较。 |
| `CanExit` + `CanEnter` 双验证在 StateMachine 层 | A: 由 State 自己在 OnTick 里调 → 重复代码。 B: CanExit 合并到 OnTick → 违反单责。 | StateMachine 统一做双验证，State 只声明能力。打断走独立路径 `CanBeInterrupted`。 |
| GatingState 内联所有门控逻辑 | A: 注入 `AbilityGating` 工具类 → 多了依赖。 B: 用 Context 取回调 → 每个 State 都一样。 | Context 已有 `Executor`，所有回调（`IsOnCooldown`/`ConditionCallback`/`OwnedTags`）可从 ctx 取。State 只需 `_next` + `_rejected`。 |
| AcceptTags 层级匹配用 `StartsWith` | A: 用 `rTag.Matches()` — 需要构造 struct。 B: 全量标签索引 — 过重。 | `StartsWith(acceptTag + ".")` 覆盖所有层级，拖尾点防误匹配。 |

## Known Issues

- [ ] Pipeline 只有 GatingState → Completed 骨架——BeforeExe/Execute/AfterExe/CanExit 待实现（P1）
- [ ] `IdleState` 已定义但未使用——`ActiveAbilityPipeline.IsIdle` 用 `is CompletedState/RejectedState` 判定（P2）
- [ ] `Pipeline.Start()` 返回 false 时队列中的技能静默丢弃——需加错误日志或事件（P2）
- [ ] 旧 `AbilityCost.cs` / `AbilityEffects.cs` / `AbilitySearch.cs` 仍留作参考——State 类落地后应删除（P2）

## Cross-References

### Related Sessions
- [2026-06-30-property-tree-restructure.md](2026-06-30-property-tree-restructure.md) — PropertyTree Equipment 层重构，MeleeWeaponSO 路径来源

### Related Plans
- [../plans/abilitydriver-glittery-reef.md](../plans/abilitydriver-glittery-reef.md) — StateMachine 设计审批计划

### Related Tech Docs
- [tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md) — 八维度管道设计
- [tech/L2-services/L2-modules/L3-properties/property-tree-equipment.md](../tech/L2-services/L2-modules/L3-properties/property-tree-equipment.md) — Equipment 树，Weapon/ATK 路径

### Flag for Design Doc Creation
- [x] No design doc needed — internal architecture refactoring, no player-visible behavior changes.
