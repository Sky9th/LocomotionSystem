# 2026-06-30 — Ability Pipeline States 扩展

## Background

上午完成了 Pipeline 状态机框架（IState / StateMachine / GatingState / CostState），但管线只在 CostState → CompletedState 占位。需要补齐 ③ Search 和 ④⑤ Execution/Effects 两个核心步骤，为后续 CooldownState / RecoveryState 铺路。

同时发现两个架构问题需要修正：
1. `SActiveAbilityContext` 是 struct，值拷贝导致 SearchState 写入的 `Targets`/`Hits` 对下游不可见
2. `AbilityState` 命名暗示领域绑定，实为通用 Pipeline State 模式

## Changes

### Pipeline States 扩展
- 新建 `SearchState.cs` — ③ 搜索命中（从 `ExecutionState` 拆分），`AbilitySearch` 逻辑内联为 `private static` 方法
- `ExecutionState.cs` — 保留但缩减为仅 ⑤ 效果载荷 + 逐 hit `AbilityReactor.Resolve` 调用
- 管线顺序调整为 `Gating → Search → Cost → Execution`（动作游戏范式：先确认目标再决定是否消耗）
- 枚举 `EActiveAbilityState` 插入 `Search = 3`，Execution → 4，Cooldown → 5，Recovery → 6

### ref TContext 零拷贝架构
- `IState<TContext>` — 全部 7 个方法签名改为 `ref TContext ctx`
- `StateMachine<TContext>` — `Start(ref)` / `Tick(ref)` / `Interrupt(ref)` / `Transition(ref)`
- `AbilityPipelineState`（原名 `AbilityState`） — 全部虚方法 `ref SActiveAbilityContext`
- 所有 7 个 State 子类签名同步更新
- `ActiveAbilityPipeline` — 3 处调用点显式 `ref _ctx`
- `SActiveAbilityContext` 保持 struct — 零 GC，零拷贝，链上写入直接可见

### Debug 可视化
- `SearchState` 最小停留 0.5s + 每帧 `Debug.DrawRay/Line`（duration 0.5s）
- Cone: 黄色中心线 + 左右边缘 + 16 段远端弧线
- Ray: 红色单线；Circle: 青色 32 段 XZ 圆环
- 白色三环原点球体 — 始终绘制，确认 State 在运行
- 方向兜底：`direction` 为零向量时默认 `Vector3.forward`

### Bug 修复
- **SearchImportExport**: 新建资产时漏调 `ApplyFields`，导致 `range`/`angle` 全为默认值 0
- **SearchSO 资产**: `Search_Cone_Melee_Blade_Light` range 0→2, `Search_Ray_Melee_Blade_Heavy` range 0→2.5（JSON 正确但 .asset 未同步）
- **CostState**: 移除抢占式的 `abilityTag` 挂载——冷却互斥应由 CooldownState 统一管理，否则 tag 永不过期导致后续技能全被拒

### 命名与废弃
- `AbilityState` → `AbilityPipelineState`（类名 + 文件名 + .meta）
- `AbilitySearch` 标记 `⛔ DEPRECATED` — 逻辑已内联至 `SearchState`
- `AbilityExecutor` OLD_IMPLEMENTATION region 标记 `⛔ 即将废弃`

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Search 和 Cost 顺序：Search 在前 | A: 设计文档原顺序 Cost → Activation → Search（MOBA 范式，按下即扣蓝）。 B: 不分先后，并行。 | 动作游戏需要先确认目标再决定是否消耗。空挥 / 无目标技能 Search 返回空，Cost 仍然执行（自身 Buff 等）。 |
| ref TContext 而非 class | A: class 改一行，改动最小。 B: 让 States 持有 Pipeline 引用自行取 ctx。 | ref 是泛型 FSM 的"正解"——零 GC、零拷贝、接口明确。class 简单但引入堆分配，违反 ECS 方向。 |
| Search 和 Execution 拆分 vs 合并 | A: 合并为单 State（原 `ExecutionState` 含 Search+Effects）。 | 拆分后 Search 可独立计时（0.5s 调试窗口）、独立绘制 Debug 形状、独立打断控制。两步无耦合，不应绑在一起。 |
| AbilitySearch 内联至 SearchState | A: 保留 `AbilitySearch` 类，注入 State。 B: 提为 static 工具类。 | `AbilitySearch` 只有 4 个无状态方法，内联后 SearchState 自包含、无外部依赖。旧类暂留供旧 AbilityExecutor 引用。 |
| SearchState 最小停留 0.5s | A: 单帧穿透，不做停留。 | 调试阶段需要可视化观察搜索区域。0.5s 足够在 Scene 视图确认形状、方向、范围是否正确。正式版可改为 0。 |

## Known Issues

- [ ] CooldownState + RecoveryState 尚未实现 — 当前 Search → Cost → Execution → Completed，无冷却管理（P1 — 下次 session）
- [ ] `SActiveAbilityContext` 保留 `S` 前缀（struct 命名惯例），但类名未改 — 后续可考虑 `ActiveAbilityContext`（P3 — 不影响功能）
- [ ] `AbilitySearch.cs` 与 `AbilityEffects.cs` 仍留在代码库 — 旧 `AbilityExecutor.TryActivate` 引用，Pipeline 完全接管后可删除（P2）
- [ ] Debug 形状假设纯水平方向（`Quaternion.Euler` 仅绕 Y 轴），垂直瞄准未覆盖（P3 — 俯视角够用）

## Cross-References

### Related Sessions
- [2026-06-30-ability-pipeline-state-machine.md](2026-06-30-ability-pipeline-state-machine.md) — Pipeline 框架本日第一 Session（IState / StateMachine / Gating / Cost）

### Related Plans
- [../plans/staged-enchanting-blum.md](../plans/staged-enchanting-blum.md) — ExecutionState → CooldownState → RecoveryState 实现计划

### Related Tech Docs
- [tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md) — 八维度管道设计（②③④⑤对应 States）

### Flag for Design Doc Creation
- [x] No design doc needed — internal architecture expansion, no player-visible behavior changes.
