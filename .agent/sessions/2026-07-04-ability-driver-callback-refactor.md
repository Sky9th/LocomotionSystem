# 2026-07-04 — AbilityDriver 回调模式重构 + Brain API 去 Driver 参数

## Background

`AbilityDriver` 是空壳——`OnStarted` 只播 clip，无反馈机制。管道侧需要知道"激发帧到了吗""动画播完了吗"，但 Driver 没有给外部提供任何查询或回调入口。

同时 `AnimationBrain.SubmitRequest(ICharacterAnimationDriver driver, AnimationRequest request)` 要求调用方持有 Driver 引用，导致 L3_Ability 侧必须 import Driver 类型，违反分层原则。

## Changes

### AnimationRequest — 回调注入 + 路由字段
- 新增 `EDriverType` 枚举（Ability / Traversal）
- 新增 `DriverType` 字段 — Brain 据此路由请求到对应 Driver
- 新增 `CustomData`（object）— 从 AnimationRequest 传 AbilityActivationSO 到 Driver，不破坏跨层依赖
- 新增 3 个 `System.Action` 回调：`OnMarker`（激发帧）、`OnCompleted`（播完）、`OnInterrupt`（被打断）

### AbilityDriver — 纯 ICharacterAnimationDriver 实现
- 移除 `SubmitAbility()` 方法 — 请求构建不再由 Driver 负责
- `OnStarted` 从 `request.CustomData` 取 AbilityActivationSO 设 Speed，注入 Animancer 事件在 windupDuration/clipLength 归一化位置调 `OnMarker`
- `OnCompleted` / `OnInterrupted` 调对应回调，保存 `_currentRequest` 引用避免 Arbiter 清理 `activeRequest` 后无法访问

### AnimationBrain — 收窄 Driver 管理 API
- 新增 `SubmitRequest(AnimationRequest request)` 重载 — 按 `DriverType` 解析对应 Driver 后提交，调用方无需传 Driver
- `Release(ICharacterAnimationDriver driver)` → `Release()` — 无参，释放当前活跃 Driver
- `RegisterDriver` / `UnregisterDriver` 保留 driver 参数（内部注册机制，仅 BaseAnimationDriver 调用）

### DriverArbiter
- 删除 `Release(ICharacterAnimationDriver driver)` — 原有验证逻辑 `driver == activeDriver` 无实际价值
- 新增 `ReleaseActive()` — 检查 `activeRequest != null` 后释放，更语义化

## Decisions

| Decision | Rationale | Rejected Alternative |
|----------|-----------|---------------------|
| 回调放 AnimationRequest 而非 Brain 门面方法 | 回调是调用方—Driver 之间的契约，Brain 只是快递员，不应感知回调逻辑 | Brain 暴露 `OnAnimationMarker` event — 全局事件粒度太粗，多技能并发时无法区分 |
| `EDriverType` 路由而非 Brain 自动匹配 ChannelMask | 多个 FullBody Driver（Ability/Traversal）无法通过 ChannelMask 区分 | 不用路由，Driver 自己调 `brain.SubmitRequest(this, request)` — 要求外部持有 Driver，违反了分层 |
| 移除 `SubmitAbility`，外部直接构建 AnimationRequest | Driver 只管播放，请求构建是谁调用谁负责。单一职责 | 保留 `SubmitAbility` 在 Driver — 混合了构建和提交，Driver 职责不纯 |
| `ReleaseActive()` 而非 `Release(driver)` | 同一时间只有一个活跃 Driver，传参冗余 | 保留 `Release(driver)` 加验证 — 验证无意义，Arbiter 自己知道谁活跃 |

## Known Issues

- [ ] TraversalDriver 目前未实现请求提交，`EDriverType.Traversal` 路由就绪但无人调用
- [ ] `OnInterrupt` 回调在 Arbiter 的 ProcessQueue 中断路径和 ReleaseActive 路径均会触发，需调用方处理幂等
- [ ] Animancer Events 的 `this` 作 key 与 BaseLayer 脚步事件同一模式，存在相同风险——事件序列在 clip 切换时可能残留（已知问题，非本次引入）

## Cross-References

### Related Sessions
- [2026-07-04-ui-weapon-ability-bar-prefabs.md](2026-07-04-ui-weapon-ability-bar-prefabs.md) — 同日 UI 工作，暂停后回归管道
- [2026-07-03-rdtag-rename-animation-clip-ability-pipeline.md](2026-07-03-rdtag-rename-animation-clip-ability-pipeline.md) — animationClip 字段类型从 StringAsset 改为 AnimationClip

### Related Tech Docs
- [tech/L2-services/L2-modules/L3-character/animation/](../../tech/L2-services/L2-modules/L3-character/animation/) — AnimationBrain, DriverArbiter, ICharacterAnimationDriver, BaseCharacterAnimationDriver

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactor, no design-facing changes.
