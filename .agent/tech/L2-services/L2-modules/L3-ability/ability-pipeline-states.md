# Ability Pipeline — 状态机实现

> **Last Verified**: 2026-06-30 | **Verification**: All referenced files exist, signatures match code

## Layer Position

L3 → L4（领域子系统的子系统）。位于 `L3_Ability/State/` 和 `L3_Ability/Executor/State/`。

- `State/` — 泛型层（`IState<TContext>`, `StateMachine<TContext>`, `AbilityPipelineState`），零领域依赖，可提至 Shared/
- `Executor/State/` — 领域绑定层（具体 Pipeline 步骤）

## Call Chain

```
AbilityExecutor.Update()
  └─ ActiveAbilityPipeline.Tick(ref _ctx, dt)
       └─ StateMachine<SActiveAbilityContext>.Tick(ref ctx, dt)
            └─ current.OnTick(ref ctx, dt)          ← 返回自身=停留 / 返回新State=流转
                 └─ CanExit(current, ref ctx) + CanEnter(next, ref ctx) 双验证
                      └─ Transition(next, ref ctx)
                           └─ OnExit(current, ref ctx) → OnEnter(next, ref ctx)

GatingState.OnTick         → SearchState / RejectedState
SearchState.OnTick         → CostState                   （0.5s 最小停留）
CostState.OnTick           → ExecutionState / RejectedState
ExecutionState.OnTick      → CompletedState              （TODO: → CooldownState）
CompletedState / RejectedState / IdleState               （终态，永远返回自身）
```

## State 流转表

| State | Id | OnTick 返回 | 条件 |
|-------|-----|------------|------|
| `GatingState` | 1 | `SearchState` | 冷却/互斥/外部条件全通过 |
| | | `RejectedState` | 任一闸门失败 |
| `SearchState` | 2 | `CostState` | 等待 0.5s 最小停留 |
| `CostState` | 3 | `ExecutionState` | 资源预检+扣除通过 |
| | | `RejectedState` | 资源不足或回调缺失 |
| `ExecutionState` | 4 | `CompletedState` | 效果施加完毕（TODO: → CooldownState） |
| `IdleState` | 0 | 自身 | 终态 |
| `RejectedState` | 8 | 自身 | 终态 |
| `CompletedState` | 7 | 自身 | 终态 |

## Coupled Modules

| 方向 | 模块 | 关系 |
|------|------|------|
| → 消费 | `AbilityExecutor` | 通过 `SActiveAbilityContext.Executor` 取回调（`IsOnCooldown`, `PeekStatCallback`, `ModifyStatCallback` 等） |
| → 消费 | `AbilitySearchSO` | SearchState 内联物理查询分发（Cone / Ray / Circle） |
| → 消费 | `AbilityEffects` | ExecutionState 调用 `ApplySelf()` + `BuildDamageInfo()` |
| → 消费 | `AbilityReactor` | ExecutionState 逐 hit 调 `Resolve()` 落地伤害 |
| ← 被调用 | `ActiveAbilityPipeline` | 持有 `StateMachine<SActiveAbilityContext>`，组装启动链 |
| ← 被调用 | `AbilityExecutor` | `Update()` 中驱动 `_pipeline.Tick(ref _ctx, dt)` |

## Public API

### IState<TContext>（泛型接口）

```csharp
public interface IState<TContext>
{
    bool CanEnter(ref TContext ctx);          // 流转进入前验证
    bool CanExit(ref TContext ctx);           // 流转离开前验证
    bool CanBeInterrupted(ref TContext ctx);  // 打断前验证
    void OnInterrupted(ref TContext ctx);     // 被打断时清理
    void OnEnter(ref TContext ctx);           // 进入时初始化
    void OnExit(ref TContext ctx);            // 离开时清理
    IState<TContext> OnTick(ref TContext ctx, float dt);  // 每帧驱动
}
```

> **ref TContext**: 2026-06-30 全链改为 `ref` 传递。struct context 零拷贝，State 间写入直接可见。

### StateMachine<TContext>（泛型状态机）

```csharp
public class StateMachine<TContext>
{
    public IState<TContext> Current { get; }
    public IState<TContext> Previous { get; }
    public float StateTime { get; }                        // 进入当前 State 后累计秒数

    public bool Start(IState<TContext> first, ref TContext ctx);
    public void Tick(ref TContext ctx, float dt);
    public bool Interrupt(IState<TContext> target, ref TContext ctx);
}
```

- `Start` — 调 `first.CanEnter(ref ctx)` 验证，失败返回 false
- `Tick` — 每帧：`OnTick` → `CanExit` + `CanEnter` → `Transition`。`ctx` 由 Pipeline 方持有，全程 ref 传递
- `Interrupt` — 外部打断：`CanBeInterrupted` → `OnInterrupted` → `Transition`。打断不能拒绝（`CanBeInterrupted` 为唯一闸门）

### AbilityPipelineState（抽象基类）

```csharp
public abstract class AbilityPipelineState : IState<SActiveAbilityContext>
{
    public abstract EActiveAbilityState Id { get; }           // 外部通过枚举判断当前阶段

    // 全部虚方法提供默认实现
    public virtual bool CanEnter(ref SActiveAbilityContext ctx) => true;
    public virtual bool CanExit(ref SActiveAbilityContext ctx) => true;
    public virtual bool CanBeInterrupted(ref SActiveAbilityContext ctx) => true;
    public virtual void OnInterrupted(ref SActiveAbilityContext ctx) { }
    public virtual void OnEnter(ref SActiveAbilityContext ctx) { }
    public virtual void OnExit(ref SActiveAbilityContext ctx) { }
    public abstract IState<SActiveAbilityContext> OnTick(ref SActiveAbilityContext ctx, float dt);
}
```

## 具体 State 类

### GatingState（② 门控检查）

- **文件**: `Executor/State/GatingState.cs`
- **逻辑**: 冷却 → 互斥 → 外部条件，三道闸门串联
- **公开方法**: `internal static string ResolveCooldownKey(AbilitySO)` — 供 CooldownState 复用
- **拒绝时输出** `Debug.LogWarning` 含具体原因

### SearchState（③ 搜索命中）

- **文件**: `Executor/State/SearchState.cs`
- **逻辑**: 首帧执行物理查询（Cone / Ray / Circle 分发），填充 `ctx.Targets`
- **最小停留**: 0.5s — 给 Debug 可视化观察窗口
- **Debug 绘制**: 每帧 `Debug.DrawRay/Line`（duration 0.5s），颜色按形状区分（黄=锥，红=射线，青=圆），白色原点球体始终可见
- **内联**: `AbilitySearch` 的 4 个方法内联为 `private static`

### CostState（④ 资源消耗）

- **文件**: `Executor/State/CostState.cs`
- **逻辑**: Phase 1 预检（全部可负担？）→ Phase 2 扣除
- **预检失败** → `RejectedState`
- **注意**: 不再挂 abilityTag — 冷却互斥由 CooldownState 统一管理

### ExecutionState（⑤ 效果载荷）

- **文件**: `Executor/State/ExecutionState.cs`
- **逻辑**: `AbilityEffects.ApplySelf()` → `BuildDamageInfo()` → 逐 hit `AbilityReactor.Resolve()`
- **当前终点**: `CompletedState`（TODO: → CooldownState）

### TerminalStates（终态）

- **文件**: `Executor/State/TerminalStates.cs`
- `IdleState` (0): 未启动时的终态
- `CompletedState` (7): 正常完成
- `RejectedState` (8): 门控/资源拒绝
- 三者 `OnTick` 永远返回自身

## Design Decisions

| Decision | Reason |
|----------|--------|
| ref TContext 而非 class | struct + ref = 零 GC 零拷贝，泛型 FSM 的正解 |
| OnTick 返回自身 = 停留 | 最轻量语义，StateMachine 只做 `next != current` 比较 |
| Search 和 Cost 顺序：Search 在前 | 动作游戏范式——先确认目标再决定是否消耗 |
| State 内 `new` 下一站 | State 自组装，Pipeline 不持有链式结构 |
| `AbilityPipelineState` 命名 | 点名归属 Ability Pipeline，比 `AbilityState` 更精准 |
| SearchState 内联 AbilitySearch | 4 个无状态方法，自包含优于外部依赖 |

## Future Plans

| Plan | Status | Source |
|------|--------|--------|
| CooldownState | TODO — 冷却施加 + cooldownAbilityTags 映射 + CleanupExpiredCooldowns 自动清理 | short-term plan |
| RecoveryState | TODO — 后摇计时，canCancelRecovery 打断控制 | short-term plan |
| SearchState MinDuration 移除 | 正式版改为 0（单帧穿透） | 调试阶段临时 |
| AbilitySearch.cs 删除 | Pipeline 完全接管后，旧 TryActivate 删除时一并清理 | DEPRECATED 标注 |
| AbilityEffects.cs 内联至 ExecutionState | 与 SearchState 同等对待 | 远期 |
