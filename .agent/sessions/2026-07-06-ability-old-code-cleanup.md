# 2026-07-06 — Ability 旧代码全量清理

## Background

Ability Pipeline 8 State（Gating→Cost→Windup→Cooldown→Execution→Recovery→Completed/Rejected）已在 v0.38.0-v0.38.4 全量接管技能执行。`AbilityExecutor.cs` 中的 `#region OLD_IMPLEMENTATION`（328 行）混有死代码和仍被新管道使用的活跃基础设施，自 v0.31.3 标记"即将废弃"后已累积 6 个版本。旧 `TryActivate` 单体方法、`ExecutePassive`、`AbilitySearch` 类、`SearchState` 管线状态均已无调用者。

本次会话彻底清理，净减少 575 行代码。

## Changes

### AbilityExecutor.cs — 主战场
- 删除 `#region OLD_IMPLEMENTATION` 及 14 个死成员：`initialPassives`、`runtimePassives`、`_search`、`Awake()` 旧初始化、`TargetFilterCallback`、`PassTargetRequiredTag()`、`PassCooldown()`、`ApplyCooldown()`、`ExecutePassive()`、`ApplyEffects()`（被动重载）、`AddPassive()`/`RemovePassive()` 空桩、`TryActivate()`、`ApplyBuff()`
- 15 个活跃成员提升至类主体区域：`OwnedTags`、5 个回调委托（`OutgoingDamageCallback`/`OnHitResolved`/`GatingConditionCallback`/`PreviewCostCallback`/`ApplyCostCallback`）、冷却系统全套（`cooldownEndTimes`/`_buffTags`/`CleanupExpiredCooldowns`/`AddCooldown`/`StartCooldown`/`IsOnCooldown`/`AddBuffTags`）、Unity 物理回调（`OnTriggerEnter`/`OnTriggerExit`）

### 废弃文件删除
- `Executor/AbilitySearch.cs` + `.meta` — 物理查询已内联至 ExecutionState
- `Executor/State/SearchState.cs` + `.meta` — 不在当前管道链中，无代码实例化

### 附带清理
- `EActiveAbilityState.cs` — 移除 `Search = 3` 废弃枚举值
- `Trap.cs` — 移除 `TargetFilterCallback` 赋值、`ShouldTrigger()` 方法、死字段 `targetLayers`；更新 XML doc 反映新管道路径
- `PassiveAbilitySO.cs` — tooltip `TargetFilterCallback` → `GatingConditionCallback`
- `ExecutionState.cs` — 清理过期注释和 `#region Search` 标记

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 将活跃代码移出 region 整体替换 | A: 保留 region 仅逐行删除死代码 → region 内行号混乱，逐行删容易出错，且 region 标记本身就暗示"旧代码"，保留会造成混淆 | 一次性替换干净利落，活跃成员在新位置有清晰注释分区 |
| 删除 `TargetFilterCallback` 而非迁移 | A: 在新管道中重新实现过滤逻辑 → 当前无实际需求，被动管道通过 `AbilitySearchSO.targetMask` 和 `GatingConditionCallback` 已覆盖过滤场景 | 死回调不应保留；Trap 的层过滤可通过被动技能的 search 配置实现 |

## Known Issues

- [ ] Trap 的 `targetLayers` 层过滤功能已随 `ShouldTrigger()` 一同删除 — P2 — 被动技能的 `AbilitySearchSO.targetMask` 可替代实现层过滤，但 Trap 组件不再提供独立的 Inspector 层过滤入口
- [ ] `PathFinding.unity` 场景中 Trap GameObject 的 `targetLayers` 序列化值会丢失 — P2 — Unity 会在下次加载场景时自动清理丢失的序列化字段

## Cross-References

### Related Sessions
- [2026-07-06-passive-pipeline-runtime.md](2026-07-06-passive-pipeline-runtime.md) — 被动管线运行时接入，SyncInstances 桥接
- [2026-07-06-pipeline-animation-fixes.md](2026-07-06-pipeline-animation-fixes.md) — 管道动画卡死修复，为本次清理扫清最后障碍

### Related Plans
- [../plans/short-term.md](../plans/short-term.md) — S3.5 旧代码清理任务标记为完成

### Related Tech Docs
- [../tech/L2-services/L2-modules/L3-ability/ability-pipeline-states.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-states.md) — 管道状态文档，待更新删除 SearchState 引用

### Flag for Design Doc Creation
- [x] No design doc needed — pure internal refactor, no player-facing behavior changes.
