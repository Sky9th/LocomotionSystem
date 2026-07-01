# 2026-07-02 — Animation 仲裁重构 + AbilityDriver + ArmPoseLayer

## Background

前一阶段完成 Ability Pipeline 审计修复后，动画接入需求暴露了 DriverArbiter 架构问题：Arbiter 同时做仲裁和播放（AcceptRequest 中调 layer.Play），Driver 被架空。同时 LocomotionDriver.Evaluate 跨层管理 Arm，逻辑混杂。需要重构仲裁模型并实现通用动画 Driver。

## Changes

### DriverArbiter 重构
- AcceptRequest 移除 `layer.Play()` — 播放权归还 Driver
- `OnStarted()` → `OnStarted(AnimationRequest request)` — 数据由上至下传递，不反向查 brain
- OnInterrupted 去重 — 统一由 ProcessQueue 调用，AcceptRequest 不再重复
- 同 Driver 替换也调 OnInterrupted（修复 IgnoreCollision 泄漏）
- 默认驱动被抢占时调 OnInterrupted（修复 Arm 武器姿态残留）
- Release 前调 OnInterrupted（修复 driver 无清理机会）
- CheckCompletion 对 Stay 也调 OnCompleted
- ProcessQueue 稳定排序（类型名二级排）+ snapshot 防并发 + 丢弃日志
- UnregisterDriver 清 activeDriver 僵尸引用
- `CanInterrupt` 方法：同类可打断，异类互斥（Ability ↔ Traversal）

### 新模块
- **AbilityDriver** — 通用一次性动画驱动，OnStarted 自播 layer.Play，被动响应 Arbiter
- **ArmPoseLayer** — Arm 层武器姿态独立封装，LocomotionDriver 不再触碰 Arm 层

### LocomotionDriver 重构
- BaseLayer 内化 `EvaluateAnimSet` — 自决 AnimSet/IdleOverride，不再由 Driver 外部喂
- `IdleOverride` 机制：partial grip 静止时覆盖 defaultSet idle
- LocomotionDriver.Evaluate 清空，退化为纯持有者（Drive 透传 baseLayer + armPoseLayer）
- OnInterrupted/OnResumed 通知 ArmPoseLayer

### 接口变更
- `ICharacterAnimationDriver.OnStarted()` → `OnStarted(AnimationRequest request)` 触达全部 Driver

### 测试
- CharacterActor T 键临时测试 AbilityDriver

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 播放权归还 Driver | A: Arbiter 继续代播。B: Driver OnStarted 自播 | Arbiter 应只仲裁不播放，Driver 控制层是职责所在 |
| OnStarted 加参数而非查 brain | A: 不改签名，Driver 反向查 brain.ActiveRequest | 违反"数据由上至下"原则，直接调 OnStarted 会静默失败 |
| ArmPoseLayer 下沉到 LocomotionDriver | A: 放 AnimationBrain 独立。B: 放 LocomotionDriver | Arm 只在 Locomotion 活跃时有意义，Drive 不跑时自然休眠 |
| 异类互斥（Ability ↔ Traversal）| A: Resistance 竞争。B: 类型互斥 | 简单明确，后续可加 CanInterrupt 字段升级 |
| BaseLayer 内化 EvaluateAnimSet | A: Driver 外部评估再传入 | 与 ArmPoseLayer 平权——各读 Context，各管各的 |

## Known Issues

- [ ] Gait vs Phase 时序不一致 — 松手减速期间武器姿态短暂消失 (P2 — Locomotion 层解决)
- [ ] ChannelMask 未被 Arbiter 消费 — 所有 Driver 共争 FullBody (P3 — 需 ArmArbiter)
- [ ] T 键测试代码待移除
- [ ] BaseLayer/ArmPoseLayer 决策逻辑重复 (P3 — 已讨论方案 B 暂不修)

## Cross-References

### Related Sessions
- [2026-07-01-ability-pipeline-audit.md](2026-07-01-ability-pipeline-audit.md) — 前一阶段：Pipeline 审计修复
- [2026-07-01-ability-pipeline-full-chain.md](2026-07-01-ability-pipeline-full-chain.md) — Pipeline 8 State 框架落地

### Related Plans
- [../../plans/async-cuddling-rain.md](../../plans/async-cuddling-rain.md) — AbilityDriver 完整计划 v2（保留备用）
- [../../plans/animation-driver-minimal.md](../../plans/animation-driver-minimal.md) — 最小化执行计划

### Related Tech Docs
- [../tech/L2-services/L2-modules/L3-ability/ability-pipeline-states.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-states.md) — Pipeline 文档

### Flag for Design Doc Creation
- [x] No design doc needed — internal architecture refactor, no player-facing changes.
