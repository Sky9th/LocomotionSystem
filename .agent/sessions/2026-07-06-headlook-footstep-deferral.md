# 2026-07-06 — Head Look + Footstep 延后清理

## Background

Head Look IK 和 Footstep 事件桥接对俯视角生存游戏优先级非常低——玩家视角自上而下，角色面部朝向和脚步同步几乎不可见。旧 Vector2MixerState 方案从未生效：`headLookMixer` 字段始终为 null，`UpdateHeadLook()` 在运行时第一行就 return。Footstep 桥接线虽已就位但无实际消费方（除非机器人等特殊角色）。

本次清理旧代码、注释桥接线，保留基础设施占位供将来复用。

## Changes

### Head Look 全量删除
- 删除 `CharacterHeadLook.cs` + `.meta`（49行静态工具类：`Evaluate` / `EvaluatePlanarHeading` / `NormalizeAngle180`）
- `AnimationBrain.cs` — TotalLayerCount 7→6；移除 `HeadLook=5` 常量（Footstep 重编号为 5）；移除 `headLookLayer` 字段、`headLookMixer`/平滑字段（4个）；移除 `UpdateHeadLook()` / `FreezeHeadLookChildren()` 方法；移除 `HeadLookLayer` 公共属性；`Apply()` 中注释 `UpdateHeadLook(ctx)` 调用
- `CharacterKinematic.cs` — `CharacterHeadLook.Evaluate()` → `Vector2.zero` + 注释
- `LocomotionAnimationConfigSO.cs` — 移除 `headLookSmoothingSpeed` 字段
- `AnimationImportExport.cs` — DTO + Export + Import 三处移除 `headLookSmoothingSpeed`

### Footstep 桥接注释
- `AnimationBrain.Start()` — 注释 `FootstepCallback` 桥接线，保留 `OnFootstep` 事件签名
- `CharacterAudio.OnWire()` / `OnDestroy()` — 注释订阅/取消订阅，保留 `HandleFootstep()` 方法

### 计划更新
- S5.1 Footstep → 🔒 延后
- S5.2 Head Look IK → 🔒 延后
- S5 耗时 ~2.5天 → ~1天

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 注释 Footstep 桥接而非删除 | A: 完全删除 `OnFootstep` 事件 + `FootstepCallback` + `InjectFootstepEvents` → 牵连 BaseLayer 内部基础设施，删除面过大且无收益 | 保留基础设施，将来特殊角色（机器人）取消注释即可 |
| Head Look 全量删除而非注释 | A: 注释保留所有 dead code → 徒增代码量，且已有明确替代方案（Animation Rigging MultiAimConstraint） | 代码已死（mixer 从未赋值），删除比注释更干净 |

## Known Issues

- [ ] `CharacterBuildContext.HeadMask` / `CharacterActor.headMask` 字段仍序列化但不再被 AnimationBrain 消费 — P2 — 将来 IK 实现时重新接线
- [ ] `CharacterConst.MaxHeadYaw` / `MaxHeadPitch` 属性路径不再被读取 — P2 — 属性定义保留，将来 IK 可能需要

## Cross-References

### Related Sessions
- [2026-07-06-ability-old-code-cleanup.md](2026-07-06-ability-old-code-cleanup.md) — 同一天旧代码清理，本次延续

### Related Plans
- [../plans/short-term.md](../plans/short-term.md) — S5.1/S5.2 标记延后

### Related Tech Docs
- 动画系统模块文档待更新（AnimationBrain 层数 7→6、HeadLookLayer 移除）

### Flag for Design Doc Creation
- [x] No design doc needed — internal cleanup and deferral, no player-facing changes.
