# 2026-06-22 — AbilityReactor 生命周期修复 + GripTag 解耦

## Background

场景预置 NPC 在 Awake 阶段报错 `[EventHub] Channel 'HitEventSO' not found`。根因是 AbilityReactor（普通 MonoBehaviour）在 Awake 中调 `EventHub.Get<HitEventSO>()`，与 EventHub.Awake 的执行顺序不确定——AbilityReactor 可能先跑，此时 EventHub 的 lookup 尚未构建。

Player 不受影响的原因：所有 `EventHub.Get<T>()` 调用都在 OnWire 阶段（由 `ModuleHub.Start()` 驱动），此时所有 Awake 已完成。

修复 EventHub 竞态后，NPC Update 因 `GripAnimationTableSO.Resolve()` NRE 再次崩溃——grip tag 寄生在 `AbilityExecutor.OwnedTags`，NPC 没有 AbilityExecutor，ownedTags 为 null。装备状态不应依赖技能系统。

## Changes

### AbilityReactor 生命周期修复
- AbilityReactor: 继承从 `MonoBehaviour` 改为 `ModuleChildMono`，删除 `Awake()`，`hitEvent` 解析移至 `OnWire()`
- CharacterActor: 新增 `[RequireComponent(typeof(AbilityReactor))]` 防御

### GripTag 解耦
- CharacterBuildContext: 新增 `OwnedGripTags` 属性（初始化为 `new()`，永不为 null）
- CharacterActor.Update: grip tag 消费从 `buildCtx.Ability?.OwnedTags` 切换为 `buildCtx.OwnedGripTags`
- PlayerDirector.ProcessEquipInput: grip tag 写入从 `ctx.Ability?.OwnedTags` 切换为 `ctx.OwnedGripTags`，移除 null guard

### 场景/预制体
- NPC.prefab: 清理旧组件，EventHub.channels 已有 HitEventSO
- PathFinding.unity: NPC 激活用于测试

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| AbilityReactor 改为 ModuleChildMono，hitEvent 在 OnWire 获取 | A: 只改 Awake→Start → 碎片化初始化逻辑，不如融入 ModuleHub 生命周期。B: 改 EventHub.Get<T>() 懒扫描 → 治标不治本，调用了 Get 但 channels 为空还是会报错 | 与 CharacterCombat.OnWire 模式一致，架构对齐 |
| GripTag 暂存 CharacterBuildContext | A: 新建独立 EquipmentManager → 装备系统才该做，现在过度设计。B: 加 null guard 挡 → 治标，且语义错误（装备状态寄生于技能）| BuildContext 是子模块共享数据的自然位置，TODO 标记明确是过渡方案 |
| EventHub 不改 | 曾考虑将 lookup 构建从 Awake 移 OnAssemble → 但 AbilityReactor 改 OnWire 后已消除竞争窗口，EventHub 自身无问题 | 最小改动 |

## Known Issues

- [ ] `OwnedGripTags` 是过渡方案——装备系统完成后应由 GripSwitchEvent / EquipmentManager 接管写入权（P2，见 PlayerDirector L70 TODO）
- [ ] NPC GripTable 可能未赋值 `defaultSet`——若 `GripTable.Resolve()` 返回 null 且 `DefaultLocomotionSet` 也为 null，Update 会再次崩溃（P2，待验证 NPC AnimationProfile 配置）

## Cross-References

### Related Sessions
- [2026-06-22-module-lifecycle-alignment.md](2026-06-22-module-lifecycle-alignment.md) — 本次修复依赖的 ModuleHub 生命周期对齐
- [2026-06-22-character-debug-to-event-driven.md](2026-06-22-character-debug-to-event-driven.md) — Equip 事件驱动改为当前 grip tag 写入方式

### Related Plans
- [../plans/binary-meandering-backus.md](../plans/binary-meandering-backus.md) — 本次修复的实施计划

### Related Tech Docs
- [../tech/L1-core/events/event-hub.md](../tech/L1-core/events/event-hub.md) — EventHub 文档
- [../tech/L1-core/module-system.md](../tech/L1-core/module-system.md) — ModuleHub 生命周期文档

### Flag for Design Doc Creation
- [x] No design doc needed — bugfix + internal refactor, no design-facing changes.
