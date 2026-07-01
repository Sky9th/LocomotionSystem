# 2026-07-01 — Ability Pipeline 审计修复

## Background

Ability Pipeline 8 State 框架已落地（前一会话），但多个 State 存在 TODO 占位和架构缺口：
WindupState windup 计时未实现、RecoveryState animationSpeed 未除、ExecutionState EffectCallback 未接入、
CooldownState cooldownAbilityTags 冗余映射、CostState 回调签名与 PropertyTable 架构不匹配。
本会话对全部 7 State（SearchState 已废弃）进行逐行审计并修复所有 P1/P2 项。

同时调研 AnimBrain + DriverArbiter 架构，输出 AbilityDriver 设计方案（计划文件已就绪，待下一会话执行）。

## Changes

### CostState — 架构改写
- PropertyTable 内建路径：Peek 用 `GetFloat` / Modify 用 `Modify`，覆盖 90% 常规消耗
- 回调退化为相位级排他：属性表不存在时才走 PeekStatCallback/ModifyStatCallback
- 回调签名升级为数组级：`Func<CostEffectSO[], string>` / `Action<CostEffectSO[]>`
- 预检仅对正消耗（amount > 0），负值/零跳过
- ModifyStatCallback null 检查前置到 Phase 2 入口（防止静默吞扣除）

### WindupState（原 ActivationState）— 改名 + 计时
- `ActivationState` → `WindupState`：类名/文件名/枚举值/所有引用全量改名
- `OnEnter`: 计算 `_windupDuration = windupDuration / animationSpeed`（除零防御）
- `OnTick`: 累时穿透；无前摇 → 单帧返回 CooldownState
- `CanBeInterrupted`: 检查 `canCancelWindup`
- 设计决策落地：CD 在前摇后才开始（非 Cost commit 点），PvE 生存游戏不应双罚

### RecoveryState — animationSpeed 修正
- `OnEnter`: 恢复时长从裸读 `recoveryDuration` 改为 `recoveryDuration / animationSpeed`

### ExecutionState — EffectCallback 接入
- `BuildDamageInfo` 中武器基底计算后调用 `executor.EffectCallback(effect, target, damage)`
- 外部（力量/熟练度修正）可通过回调接入伤害修正链

### CooldownState — cooldownAbilityTags 清理
- 移除 `cooldownAbilityTags` Dictionary（identity 映射永远冗余）
- 移除 `StartCooldown` 中重复的 `OwnedTags.AddTag`
- 简化 `CleanupExpiredCooldowns`，移除 identity 查重块

### AbilityExecutor
- 新增 `PropertyTable` 属性（惰性从 `GetComponent<Identity>().Properties` 取）
- 新增 `IsBlockedBySharedCooldown()` 方法（从 OLD 提取到 NEW 区域）
- 回调签名更新为数组级

### CharacterCombat
- 移除 PeekStatCallback / ModifyStatCallback 接线（PropertyTable 已覆盖，回调成死代码）

### 文档
- `ability-pipeline-states.md`：补充 WindupState/CooldownState/RecoveryState 章节，更新 Future Plans 和 Design Decisions
- `CostEffectSO.cs`：标记 TODO — 绝对值扣除不足，需 ECostMode 枚举

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| CD 在前摇后才施加 | A: Cost commit 点立即开 CD（与 Windup 并行）。B: 前摇结束时开 CD（当前） | PvE 生存游戏，被丧尸打断=已受伤+已扣体力，不应再加技能锁。Cost（体力/弹药）已是主要 spam 防线 |
| CostState PropertyTable 内建 + 回调排他 | A: 回调始终优先（旧）。B: 逐 Effect 混合查 | 属性表覆盖 90% 常规消耗；回调退化为相位级扩展点，不逐 Effect 代理 |
| 回调签名为 `CostEffectSO[]` | A: 保持单 PropertyDefSO 签名（旧）。B: 逐 Effect 循环调回调 | 回调是相位级接管，不是"另一个属性表"，应整批传入 |
| ActivationState → WindupState | 原名 Activation 语义过大（涵盖 Windup+Fire+Recovery），State 实际只做 Windup 计时 | 与 RecoveryState 粒度对齐，管线自解释 |
| Windup 结束由动画事件决定，计时兜底 | A: 纯计时（确定性）。B: 纯动画事件（精度） | 动画是时间轴 ground truth；计时器永不丢帧，作 fallback |

## Known Issues

- [ ] fireWindowDuration 未消费 — Execution 单帧触发，无持续激发窗口（P2 — Phase 4.2+）
- [ ] Windup/Recovery 动画事件未实现 — 等 AbilityDriver (Slice 3) 完成后接入
- [ ] AbilityDriver 设计方案已出（plan file `async-cuddling-rain.md` v2），待下一会话执行
- [ ] CostEffectSO 仅支持绝对值扣除，缺百分比/最大值百分比模式（TODO 已标记）

## Cross-References

### Related Sessions
- [2026-07-01-ability-pipeline-full-chain.md](2026-07-01-ability-pipeline-full-chain.md) — 前一阶段：8 State 框架落地

### Related Plans
- [../../plans/async-cuddling-rain.md](../../plans/async-cuddling-rain.md) — AbilityDriver 实现计划 v2（已审核，待执行）

### Related Tech Docs
- [../tech/L2-services/L2-modules/L3-ability/ability-pipeline-states.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-states.md) — 全面更新

### Flag for Design Doc Creation
- [x] No design doc needed — internal audit + refactor, no player-facing behavior changes.
