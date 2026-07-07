# 2026-07-07 — AirLand 分级落地 + Traversal 动画迁移 + PathfindingService 缓存修复

## Background

S3-S4 动画管线完成后，S5 动画补完剩余两项进入施工。BaseAirLandState 已有 landLight/landHard
二选一逻辑，但预留了 TODO：按 Gait 混合 LinearMixer。TraversalDriver 的 Evaluate() 和
ResolveClimbAlias() 被注释掉，标注"migrated to LocomotionAnimationSetSO traversal fields"——
实际从未完成迁移。此外 PathfindingService 无条件 Scan() 导致 A* GraphCache.bytes
缓存白费，每次进场景都重新扫描。

## Changes

### S5.4 AirLand 分级落地
- `BaseLayer.cs` — 新增 `internal AnimancerState CurrentAnimState` 属性，暴露运行时动画状态给 State 层
- `BaseAirLandState.OnEnterState()` — Play 后将 `currentAnimState` cast 为 `LinearMixerState`，按 `ctx.Discrete.Gait` 设置 `Parameter`：Idle=0, Walk=1, Run/Sprint=2

### S5.5 Traversal 动画迁移
- `TraversalDriver.Evaluate()` — 从注释状态改为完整实现。触发条件：`_isActive` 守卫 → `DesiredLocalVelocity.y > 0.1f` → 非空中 → `CanClimb && Distance < 0.3m` → Dot Product 方向验证（`dot(moveDir, -obsNormal) > 0.8`）→ 从 `brain.BuildContext.TraversalSet` 拿 `LocomotionAnimationSetSO` 选攀爬 clip → 构建 `AnimationRequest(DriverType=Traversal)` → `brain.SubmitRequest()`
- `TraversalDriver.ResolveClimbClip()` — 新增静态方法，替代旧 `ResolveClimbAlias()`。≤0.6m→climbUpHalfMeter, ≤1.1m→climbUp1meter, >1.1m→climbUp2meter。直接返回 `ClipTransition`，不再走 `AnimationAliasProfile` + `StringAsset` 路径
- `TraversalDriver.OnStarted()` — 新增 `brain.FullBodyLayer.Play(request.Clip)` 播放攀爬动画，`_isActive = true` 防重复触发
- `TraversalDriver.OnCompleted/OnInterrupted()` — 新增 `_isActive = false`

### PathfindingService 缓存修复
- `PathfindingService.OnAssemble()` — `cacheStartup && file_cachedStartup != null` 时跳过 `Scan()`，尊重 A* 在 `OnEnable` 阶段从 GraphCache.bytes 加载的缓存

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Dot Product 方向验证（`dot > 0.8`）区分正面顶墙 vs 擦墙 | A: OffMeshLink 标记方案 — 零误判但需切 FollowerEntity/RichAI。B: 简单 `CanClimb + 前进意图` — 无法区分墙角。 | Dot Product 是跨引擎标准（Unity CC `-0.85`、Godot `0.8`、Unreal `0.7-0.9`），零架构改动，只改 1 文件 |
| 硬编码 0.6f/1.1f 攀爬高度阈值 | 放 PropertyTree 或 LocomotionAnimationConfigSO | 阈值与动画资源绑定，动画师在 SO 上改 clip 时一并调整，不应分散到多个配置点 |
| GraphCache.bytes 不进 Addressable | 标记为 Addressable 异步加载 | 它是 `TextAsset` 引用，场景内 `AstarPath.data.file_cachedStartup` 直接拖拽，Unity 原生依赖解析即可 |

## Known Issues

- [ ] AIPath 不支持 `IOffMeshLinkHandler` 接口 — OffMeshLink 标记方案需等路径层升级到 FollowerEntity（P2，非阻塞）
- [ ] Motor 寻路激活时 `DesiredLocalVelocity == ActualLocalVelocity` — 无法做 stuck detection，dot product 依赖方向对齐而非速度不匹配（P2 — 当前 dot product 方案足够覆盖绝大多数场景）
- [ ] 攀爬动画未实际测试（场景无 NodeLink2 标记 + 无可攀爬墙体配置）— P1，需策划配置测试关卡

## Cross-References

### Related Sessions
- [2026-06-20-grip-switching.md](2026-06-20-grip-switching.md) — 同动画管线，S4 阶段

### Related Plans
- [../plans/short-term.md](../plans/short-term.md) — S5 动画补完计划

### Related Tech Docs
- [../tech/L2-services/L2-modules/L3-character/L4-animation/drivers/traversal/traversal-driver.md](../tech/L2-services/L2-modules/L3-character/L4-animation/drivers/traversal/traversal-driver.md) — 更新 Evaluate/ResolveClimbClip/OnStarted 方法
- [../tech/L2-services/L2-modules/L3-character/L4-animation/drivers/locomotion/locomotion-driver.md](../tech/L2-services/L2-modules/L3-character/L4-animation/drivers/locomotion/locomotion-driver.md) — BaseAirLandState 行为变化（不在此文档层级记录）

### Related Design Docs
- None — 内部动画实现迁移，无设计面变更

### Flag for Design Doc Creation
- [x] No design doc needed — internal animation implementation migration, no design-facing changes.
