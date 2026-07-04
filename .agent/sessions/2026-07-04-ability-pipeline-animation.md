# 2026-07-04 — Ability Pipeline 动画接入 + 冷却/输入重构

## Background

上阶段 (`v0.36.4`, `8a341203`) 完成了 AnimationRequest 回调模式重构——新增 `EDriverType` 路由、`OnMarker`/`OnCompleted`/`OnInterrupt` 回调。本次任务是技能管道侧消费这套 API：管道触发动画，动画事件驱动状态流转，计时器全程兜底。

并行完成了冷却系统标准化（abilityTag 做 key、sharedCooldownTags 改数组）、排队机制改直发（TryUse）、输入事件统一化（InputSkillEvent）。

## Changes

### 技能管道 → 动画桥接
- `AbilityExecutor` — 新增 lazy `AnimationBrain` 引用、4 个动画桥接方法（`SubmitAbilityAnimation`/`ReleaseAbilityAnimation`/`IsAnimationFireMarkerReached`/`IsAnimationClipFinished`）、`ResetAnimationFlags`、`_currentAnimRequest` 身份校验
- `WindupState` — `OnEnter` 结尾调用 `SubmitAbilityAnimation`；`OnTick` 动画 fire marker 优先退出 + 计时器兜底；新增 `OnInterrupted` 释放动画
- `RecoveryState` — `OnTick` 动画 clip 完成优先退出 + 计时器兜底；新增 `OnInterrupted`
- `ExecutionState` — 加 `TODO Phase 4.2` fireWindowDuration 占位

### 动画层修复
- `AnimationRequest` — 回调签名 `Action` → `Action<AnimationRequest>`，支持身份校验
- `AbilityDriver` — `Events(ref _fireSequence)` 替代 `Events(this, ...)` 避免 Animancer state 复用 ownership 冲突；`state.Time = 0f` 重复播放从第 0 帧开始
- `DriverArbiter` — `ProcessQueue` 简化为直接取 `queue[0]`，删除排序逻辑和 skip warning；加 TODO 受击打断

### 冷却系统重构
- `AbilitySO` — `sharedCooldownTag`（单值）→ `sharedCooldownTags`（数组）；删除 `GetCooldownKey()` 方法
- `AbilityExecutor` — 冷却直接使用 `abilityTag.FullTag` 做 key；`StartCooldown` 分离独立冷却 + loop 联动冷却；删除 `cooldownAbilityTags` 冗余映射；`IsBlockedBySharedCooldown` 接收数组
- `GatingState` — 独立冷却 `IsOnCooldown(abilityTag.FullTag)`；联动冷却传入数组；`Passed` 日志删除
- `CooldownState` — 删除重复的 `AddCooldown(sharedTag)`（`StartCooldown` 已包办）

### 排队改直发
- `AbilityExecutor` — 删除 `Queue<SQueuedSkill>` + `SQueuedSkill` 结构体；`Enqueue` → `TryUse`（空闲/拒绝时直接启动，运行中忽略）；`ActiveAbilityPipeline.IsIdle` 排除 `RejectedState`
- `EntityCommandModule` — `Enqueue` → `TryUse` + 调用前 `Pathfinding.Stop()`

### 输入事件统一化
- `SButtonInputPayload` — 新增 `BindingIndex` 字段
- `InputSkillEvent` — 新建，替代 N 个 `InputSkillNEVent`
- `InputService.BindButton` — 传递 `GetBindingIndexForControl`
- `PlayerService` — 单条 `BindInput<InputSkillEvent>` 替代 Skill1/Skill2
- `InputSkill1Event` / `InputSkill2Event` — ⛔ DEPRECATED
- `AbilityBarOverlay` — 硬编码 Q~U 七键

### 资产管理
- `sharedCooldownTag` → `sharedCooldownTags` — 12 个 .asset + `abilities_all.json` 批量更新
- 删除 `InputSkill1.asset` / `InputSkill2.asset` / `InputSkill3.asset`
- 新建 `InputSkill.asset`

### 管道清洁
- `CostState` / `CooldownState` / `GatingState` / `WindupState` / `RecoveryState` / `ExecutionState` / `SearchState` / `ActiveAbilityPipeline` — 删除所有 `Debug.Log` 信息日志，仅保留 `Debug.LogWarning/Error` 拒绝提示

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 动画 flag 放 Executor 不放 State | State 每帧创建新实例 | 回调闭包需要稳定引用；Executor 存活整个管道生命周期 |
| 计时器始终兜底 | 动画和计时器二选一 | 动画可能失败（Animancer state 复用、clip 缺失）；计时器保证流转不卡死 |
| 冷却直接用 abilityTag.FullTag 做 key | A: 拼字符串 `Ability.Cooldown.{name}` → 和 OwnedTags 不一致。B: 保留 cooldownAbilityTags 映射 → key 就是 tag，映射冗余 | 简化数据模型，删除 60 行冗余代码 |
| `Events(ref _fireSequence)` 替代 `Events(this, ...)` | A: `Destroy()` 旧 state → 破坏 locomotion。B: `new object()` 做 key → AssertOwnership 仍冲突 | ref Sequence 是官方文档指定方式——"多个调用方轮流复用同一 state 时使用" |
| `Action<AnimationRequest>` 回调带参 | A: generation counter 检查 → 多一个字段。B: 完全信任 Driver → 已出现误触发 | 回调自己验身份，防御层放在消费者端 |
| `TryUse` 替代 Enqueue + Queue | 排队机制当前无实际用途（不考虑同时释放） | 简化 Update 逻辑，减少间接层 |
| `IsIdle` 排除 RejectedState | 拒绝后 IsIdle 立刻为 true → 输入持续时死循环扣体力 | Rejected 不是"可以接受新技能"的状态 |

## Known Issues

- [ ] RejectedState 管道卡在拒绝态——后续 Enqueue/TryUse 需处理拒绝态重置。当前 TryUse 接受 Rejected 后的直接启动
- [ ] `state.Events()` 在 state 复用时返回 false 但 events 仍可用——已用 ref 重载解决 ownership，但 `HasOwnEvents` 残留问题可能存在
- [ ] `sharedCooldownTags` 数组编辑器——Inspector 原生数组暴露，IDE 内无自定义编辑器
- [ ] FadeIn=0.1s 在 crossfade 期间 `layer.CurrentState` 仍指向旧 state——当前不影响逻辑（因为有身份校验），但视觉上可能有一帧闪烁

## Cross-References

### Related Sessions
- [2026-07-04-ability-driver-callback-refactor.md](2026-07-04-ability-driver-callback-refactor.md) — 上阶段 AnimationRequest 回调模式重构 (v0.36.4)
- [2026-07-03-rdtag-rename-animation-clip-ability-pipeline.md](2026-07-03-rdtag-rename-animation-clip-ability-pipeline.md) — animationClip 字段类型从 StringAsset 改为 AnimationClip

### Related Tech Docs
- [tech/.../L3-ability/](../tech/L2-services/L2-modules/L3-ability/) — AbilityExecutor, Pipeline, States
- [tech/.../L4-animation/](../tech/L2-services/L2-modules/L3-character/L4-animation/) — AnimationBrain, DriverArbiter, AnimationRequest
- [tech/.../L5-drivers/ability/](../tech/L2-services/L2-modules/L3-character/L4-animation/drivers/ability/) — AbilityDriver

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactor, no player-facing behavior changes.
