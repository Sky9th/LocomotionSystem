# 2026-07-01 — Ability Pipeline 全 8 State 就位 + 动画链序修正

## Background

昨天 Pipeline 只有 Gating / Search / Cost / Execution 四个 State，Cooldown 和 Recovery 留作 TODO。今天补齐 Activation / Cooldown / Recovery 三个 State，并修正链序使冷却从 Animation commit 点开始计时，与 Fire + Recovery 重叠。同时修复 ExecutionState 跳转错误、AbilityExecutor 技能队列积压、StartCooldown 无覆写参数等问题。

## Changes

### 新增 State（3 个）
- `ActivationState.cs` — ③ 动画激活（Windup 前摇占位），TODO: windupDuration / animationSpeed 计时
- `CooldownState.cs` — ⑦ 冷却施加，从 Activation 入口开始计时，与 Fire+Recovery 重叠。`MinCooldown=0.05f` 防止 cooldown=0 的技能帧级连发
- `RecoveryState.cs` — ⑧ 后摇等待，TODO: recoveryDuration / animationSpeed 除法。`canCancelRecovery` 打断控制

### 链序修正
最终确认的 8 State 链：
```
Gating → Search → Cost → Activation → Cooldown → Execution → Recovery → Completed
```
- Cooldown 从 Execution 之后移至 Activation 之后——冷却在 commit 点就开始计时
- Recovery 从 Cooldown 之后移至 Execution 之后——后摇是动画收尾，与冷却重叠

### 跳转修正
- `ActivationState` → `CooldownState`（不再直连 Execution）
- `CooldownState` → 删除 recovery 判断，始终 → `ExecutionState`
- `ExecutionState` → `RecoveryState`（不再直连 Cooldown）

### AbilityExecutor 强化
- `StartCooldown(ability, overrideDuration)` — 覆写冷却参数，cooldown=0 技能用它传最小间隔
- `Enqueue` — Pipeline 运行中清掉旧排队位，只保留最新一个请求，防止技能积压

### 内联与废弃
- `ExecutionState` — `AbilityEffects.ApplySelf/BuildDamageInfo/ResolveDamageEffect` 全部内联为 private static 方法
- `AbilityEffects.cs` — 标记 ⛔ DEPRECATED

### 配置
- `AbilityActivationSO.recoveryDuration` — 新增字段，设计师手动设置，远期 AbilityDriver 自动计算
- `EActiveAbilityState` — 插入 `Activation=2`，枚举值重排

### TODO 标注
- `SearchState` — fireWindowDuration 每帧累加 (当前仅首帧，硬编码 0.5s)
- `ActivationState` — windupDuration 计时 (当前透传占位)
- `RecoveryState` — recoveryDuration / animationSpeed (当前裸读)

## Decisions

| Decision | Alternatives | Reason |
|----------|-------------|--------|
| Cooldown 在 Activation 之后而非 Execution 之后 | A: 保持原顺序 — 冷却在效果之后才开始 | 冷却语义是"不可复用"，应从 commit 点开始。Activation 入口 = commit。与 Fire+Recovery 重叠使冷却在 Recovery 结束时已走完大半 |
| Search → Cost 顺序不变 | A: 改回设计文档的 Cost→Search | 动画是连续时间轴，但决策顺序独立于动画：先找到目标再扣费仍对。Windup 是 commit 缓冲 |
| StartCooldown 用 overrideDuration 而非 minDuration | A: Mathf.Max 做下限 | 覆写语义更通用——冷却缩减/延长 modifier 直接传修正值。cooldown=0 时 CooldownState 传 0.05f 做最小间隔 |
| Enqueue 替换而非队列累积 | A: 保留无限队列 | 动作游戏不应积压技能。Pipeline 运行中只保留最新请求，队列结构保留供后续预指令 |

## Known Issues

- [ ] SearchState: fireWindow 每帧累加未实现 (P1 — 下一步)
- [ ] ActivationState: windupDuration 计时未实现 (P1 — 下一步)
- [ ] RecoveryState: animationSpeed 除法未实现 (P2)
- [ ] AnimationClip 实际播放尚未集成——三个计时 State 的 duration 来自 SO 字段而非 clip.length (P2 — 等 AbilityDriver Slice 3)
- [ ] `AbilityEffects.cs` 与 `AbilitySearch.cs` 仍留在代码库——旧 TryActivate 引用，Pipeline 全量接管后可删除 (P2)

## Cross-References

### Related Sessions
- [2026-06-30-ability-pipeline-states-expansion.md](2026-06-30-ability-pipeline-states-expansion.md) — 昨天 SearchState + ExecutionState 拆分 + ref TContext
- [2026-06-30-ability-pipeline-state-machine.md](2026-06-30-ability-pipeline-state-machine.md) — 最初状态机框架

### Related Plans
- [../plans/staged-enchanting-blum.md](../plans/staged-enchanting-blum.md) — 动画驱动 Pipeline 完整计划

### Related Tech Docs
- [tech/.../ability-pipeline-design.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md) — 八维度设计
- [tech/.../ability-pipeline-states.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-states.md) — 状态机实现

### Flag for Design Doc Creation
- [x] No design doc needed — internal pipeline completion, no player-visible changes.
