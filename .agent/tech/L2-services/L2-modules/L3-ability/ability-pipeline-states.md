# Ability Pipeline — 状态机实现

> **Last Verified**: 2026-07-06 | **Verification**: All referenced files exist, signatures match code. SearchState + AbilitySearch deleted in v0.38.5.

## Layer Position

L3 → L4（领域子系统的子系统）。位于 `L3_Ability/State/` 和 `L3_Ability/Executor/State/`。

- `State/` — 泛型层（`IState<TContext>`, `StateMachine<TContext>`, `AbilityPipelineState`），零领域依赖，可提至 Shared/
- `Executor/State/` — 领域绑定层（具体 Pipeline 步骤）

## Call Chain

```
AbilityExecutor.Update()
  └─ AbilityPipeline.Tick(ref _ctx, dt)
       └─ StateMachine<SActiveAbilityContext>.Tick(ref ctx, dt)
            └─ current.OnTick(ref ctx, dt)          ← 返回自身=停留 / 返回新State=流转
                 └─ CanExit(current, ref ctx) + CanEnter(next, ref ctx) 双验证
                      └─ Transition(next, ref ctx)
                           └─ OnExit(current, ref ctx) → OnEnter(next, ref ctx)

GatingState.OnTick         → CostState / RejectedState       （② 门控：冷却/互斥/外部条件）
CostState.OnTick           → WindupState / RejectedState （④ 资源：预检+扣除）
WindupState.OnTick     → CooldownState                   （③ 前摇：windupDuration / animationSpeed 计时，canCancelWindup 打断控制）
CooldownState.OnTick       → ExecutionState                  （冷却施加：独立 + 联动双锁，MinCooldown=0.05s 防连发）
ExecutionState.OnTick      → RecoveryState                   （④ Fire 物理查询 + ⑤ 效果载荷含 EffectCallback 修正 + 逐 hit Reactor.Resolve）
RecoveryState.OnTick       → CompletedState                  （③ 后摇：recoveryDuration / animationSpeed 计时，canCancelRecovery 打断控制）
CompletedState / RejectedState                                （终态，永远返回自身）
```

## State 流转表

| State | Id | OnTick 返回 | 条件 |
|-------|-----|------------|------|
| `GatingState` | 1 | `CostState` | 冷却/互斥/外部条件全通过 |
| | | `RejectedState` | 任一闸门失败 |
| `CostState` | 4 | `WindupState` | 资源预检+扣除通过 |
| | | `RejectedState` | 资源不足或回调缺失 |
| `WindupState` | 2 | `CooldownState` | Windup 前摇结束（windupDuration / animationSpeed） |
| `CooldownState` | 6 | `ExecutionState` | 冷却施加完成 |
| `ExecutionState` | 5 | `RecoveryState` | ④ 物理查询 + ⑤ 效果载荷（含 EffectCallback）+ 逐 hit 结算 |
| `RecoveryState` | 7 | `CompletedState` | 后摇结束（recoveryDuration / animationSpeed） |
| `RejectedState` | 9 | 自身 | 终态 |
| `CompletedState` | 8 | 自身 | 终态 |

## Coupled Modules

| 方向 | 模块 | 关系 |
|------|------|------|
| → 消费 | `AbilityExecutor` | 通过 `SActiveAbilityContext.Executor` 取回调（`IsOnCooldown`, `PeekStatCallback`, `ModifyStatCallback` 等） |
| → 消费 | `AbilitySearchSO` | ExecutionState 内联物理查询分发（Cone / Ray / Circle） |
| → 消费 | `AbilityReactor` | ExecutionState 逐 hit 调 `Resolve()` 落地伤害+Buff+Tag+事件 |
| → 消费 | `AbilityInstance` | `SActiveAbilityContext.Instance` — Owner 溯源 |
| ← 被调用 | `AbilityPipeline` | 持有 `StateMachine<SActiveAbilityContext>`，主动被动共用 |
| ← 被调用 | `AbilityExecutor` | `Update()` 中驱动 `_activePipeline.Tick` + `_runningPassives` 列表 |

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

### CostState（④ 资源消耗）

- **文件**: `Executor/State/CostState.cs`
- **逻辑**: Phase 1 预检（全部可负担？）→ Phase 2 扣除
- **两条路径互斥**（相位级排他，不混合）:
  - A. `PropertyTable` 存在 → 循环逐 Effect 内建 `GetFloat` / `Modify`（覆盖 90% 常规消耗）
  - B. PropertyTable 不存在 + 回调有接线 → 整批 `CostEffectSO[]` 交给 `PeekStatCallback`（null=通过）/ `ModifyStatCallback`
  - C. 两条路都走不通 → `RejectedState`
- **回调签名变更**（2026-07-01）:
  - `PeekStatCallback`: `Func<PropertyDefSO, float>` → `Func<CostEffectSO[], string>` — 相位级预检，null=全部通过
  - `ModifyStatCallback`: `Action<PropertyDefSO, float>` → `Action<CostEffectSO[]>` — 相位级整批扣除
- **PropertyTable 来源**: `AbilityExecutor.PropertyTable` → 惰性从 `GetComponent<Identity>().Properties` 取，null = 实体无属性系统
- **PeekStatCallback 现在是可选的**：有 PropertyTable 时走内建路径，不需要接线
- **预检失败** → `RejectedState`

### WindupState（③ 前摇计时）

- **文件**: `Executor/State/WindupState.cs`
- **逻辑**: OnEnter 计算 `_windupDuration = windupDuration / animationSpeed`，OnTick 累时穿透
- **计时公式**: 实际前摇 = 原始值 / animationSpeed（speed=1.0 为基准，防御除零）
- **无前摇时**（windupDuration ≤ 0）→ 单帧透传 `CooldownState`
- **打断控制**: `CanBeInterrupted` 看 `canCancelWindup`，false 时前摇霸体

### CooldownState（⑦ 冷却施加）

- **文件**: `Executor/State/CooldownState.cs`
- **逻辑**: 独立冷却（key=`abilityTag.FullTag`）+ 联动冷却（`sharedCooldownTag`），`MinCooldown=0.05s` 防帧级连发
- **注入**: 调 `executor.StartCooldown()` + `executor.AddCooldown()`，`CleanupExpiredCooldowns` 自动清理（0.5s 间隔）
- **cooldownAbilityTags**: 已移除 — 旧 identity 映射（key==value）冗余，NEW 代码不再需要

### ExecutionState（④⑤ 物理查询 + 效果载荷）

- **文件**: `Executor/State/ExecutionState.cs`
- **逻辑**: Fire 帧物理查询 → caster 加入 targets（self 走标准路径）→ `BuildDamageInfo()`（每 target 一个 hit，纯伤害数据）→ 逐 hit `Reactor.Resolve(hit)`（内部完成伤害+Buff+Tag+事件）
- **伤害公式**: `ComputeDamage` — 武器基底 × 技能修正 = `Σ (wd.baseValue + mod.modAdd) × mod.modMult`
- **EffectCallback**: 被 `ComputeDamage` 内联调用，同为外部（力量/熟练度）修正
- **Self-hit**: Amount=0，Reactor 通过 `hit.Target == hit.Caster` 选择 selfEffects
- **ApplySelf 已删除**: Buff/Tag 全在 Reactor 落地，Exe 不再直接改目标 PT/Tag

### RecoveryState（⑧ 动画后摇）

- **文件**: `Executor/State/RecoveryState.cs`
- **逻辑**: OnEnter 计算 `_recoveryDuration = recoveryDuration / animationSpeed`，OnTick 累时穿透
- **计时公式**: 实际后摇 = 原始值 / animationSpeed（与 Activation 一致，防御除零）
- **无后摇时**（recoveryDuration ≤ 0）→ 单帧透传 `CompletedState`
- **打断控制**: `CanBeInterrupted` 看 `canCancelRecovery`，false 时后摇霸体

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
| Search 从独立 State 退化为 Execution 内联 | 目标不是管道搜出来的——Director 外部指定。物理碰撞（流弹/散射/扇形）是 Execution 的职责 |
| State 内 `new` 下一站 | State 自组装，Pipeline 不持有链式结构 |
| `AbilityPipelineState` 命名 | 点名归属 Ability Pipeline，比 `AbilityState` 更精准 |
| SearchState 内联 AbilitySearch | 4 个无状态方法，自包含优于外部依赖 |
| Windup/Recovery 计时 ÷ animationSpeed | animationSpeed 是唯一调参旋钮。speed=1.0 时 phase marker = 实际秒数，speed=2.0 时全体快一倍。防御除零 |
| cooldownAbilityTags 移除 | 旧 identity 映射（key==value）永远冗余。独立冷却 key=`abilityTag`，联动冷却走 `AddCooldown` 直插 |

## Future Plans

| Plan | Status | Source |
|------|--------|--------|
| WindupState windup 计时 | ✅ Done 2026-07-01 — windupDuration / animationSpeed + canCancelWindup | this session |
| RecoveryState animationSpeed 除法 | ✅ Done 2026-07-01 — recoveryDuration / animationSpeed | this session |
| ExecutionState EffectCallback 接入 | ✅ Done 2026-07-01 — BuildDamageInfo 构建后调用外部修正 | this session |
| CooldownState cooldownAbilityTags 清理 | ✅ Done 2026-07-01 — 移除冗余 identity 映射 + 重复 AddTag | this session |
| AbilitySearch.cs 删除 | ✅ Done 2026-07-06 — v0.38.5 已删除文件 | this session |
| SearchState 正式删除 | ✅ Done 2026-07-06 — v0.38.5 已删除文件 | this session |
| AbilityDriver 阶段机 + 动画驱动 | Activation/Recovery State 当前用计时占位，AbilityDriver 实现后接管 | Slice 3 |
