# 2026-07-05-hit-reaction-pipeline

## Background

受击动画的数据层（`LocomotionAnimationSetSO` 新增 4 个 hitReaction 字段 + `AnimationImportExport` 序列化）已在 v0.36.9 完成，但消费链路不存在——伤害数据不携带冲击参数，`CharacterCombat.OnReaction`/`OnDamaged` 是空桩，`DriverArbiter` 有 TODO 未实现受击抢占。本次 session 完成从 `ExecutionState` → `SDamageInfo` → `CharacterCombat` → `AnimationBrain` → `DriverArbiter` → `HitReactionDriver` 的全链路。

属于 `feature/ability-pipeline` 分支的受击反应子系统交付。

## Changes

### L3_Ability — 数据入站
- `SDamageInfo` — 新增 `ImpactEffect` readonly field + 双构造函数（7-param 向后兼容，8-param 含 ImpactEffect）
- `ExecutionState.BuildDamageInfo()` — 提取 `ability.targetEffects` 中第一个 `ImpactEffectSO` 实例，传入 `SDamageInfo`
- `EHitReactionLevel` (new) — `Flinch / Stagger / Knockdown` 枚举，由资产配置受击等级
- `ImpactEffectSO` — 新增 `reactionLevel` 字段（默认 Flinch），策划在资产上直接配

### L3_Character/Animation/Requests
- `AnimationRequest.EDriverType` — 追加 `HitReaction`

### L3_Character/Animation/Drivers/HitReaction (new)
- `HitReactionDriver` — 继承 `BaseAnimationDriver`，`OnStarted` 从 `CustomData` 解包 `SHitReactionData`，临时覆写 `MixerTransition2D.FadeDuration` 后 Play，设 `MixerState<Vector2>.Parameter`
- `SHitReactionData` — 内部 struct：`MixerTransition2D Mixer + float DirX + float DirY`

### L3_Character/Animation
- `AnimationBrain.Awake()` — 注册 `HitReactionDriver` 组件
- `AnimationBrain.SubmitRequest()` — switch 追加 `HitReaction` case
- `DriverArbiter.ProcessQueue()` — 硬编码抢占规则：H1 idle→接受任意；H2 HitReaction 抢占一切（含互打断）；else 拒绝（Traversal↔Ability 互斥）

### L3_Character/Actor + Structs
- `CharacterBuildContext` — 新增 `Animation` property（`AnimationBrain`）
- `CharacterActor.Start()` — 注入 `buildCtx.Animation = characterAnimation`

### L3_Character/Combat
- `OnReaction()` — 读取 `ImpactEffect.reactionLevel` 选择动画（Flinch/Stagger/Knockdown），Knockdown 时 `OnCompleted` 链式提交 `ChainGetUp()`
- `OnDamaged()` — HP≤0 播放 `hitReactionKnockdown`（`int.MaxValue` Resistance），无 `OnCompleted`，停在倒地 pose
- `ChainGetUp()` — 起身请求（Resistance=0，可被打断）
- `WorldToLocalDirection()` — 世界 HitDirection → 本地 blend parameter（取反：伤害飞行方向 → 冲击来向）

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 受击等级由 `ImpactEffectSO.reactionLevel` 资产决定 | A: CharacterCombat 硬编码 staggerValue 阈值 → 策划无法逐技能调。B: PropertyTable 查霸体值 → 当前无霸体系统 | 资产字段让策划直接控制每个技能的受击等级，无需修改代码 |
| 抢占规则硬编码 DriverType 级别 | A: Resistance 数值比较 → 当前无 AbilityEffect 霸体机制支撑。B: 完整优先级系统 → 过度设计 | HitReaction 抢占一切（含互打断）覆盖当前需求，后续接霸体值再改用 Resistance |
| Impact Knockdown → ChainGetUp 通过 OnCompleted 链式回调 | A: HitReactionDriver 内部自动起身 → Driver 不应知道起身逻辑。B: 独立状态机 → 过度设计 | 复用现有仲裁队列，1 帧间隙可接受 |
| HP≤0 死亡播放倒地不动（无 Ragdoll） | A: 直接禁用角色 → 无视觉反馈。B: 完整 Ragdoll+EventHub 系统 → 本次 scope 过大 | 留在倒地 pose 提供清晰死亡反馈，Ragdoll 死亡系统后续 task |
| FadeIn 临时覆写 `MixerTransition2D.FadeDuration` | A: 忽略 request.FadeIn 用资产值 → 失去控制。B: 克隆 transition → 太重 | Play() 同步读取一次，覆写后立即恢复安全 |
| `WorldToLocalDirection` 取反 HitDirection | 原: 直接用 HitDirection → 方向反了（伤害飞行方向≠冲击来向） | 攻击从背后来 → 冲击将角色推向前 → blend Y=-1 匹配向前倒 |

## Known Issues

- [ ] 1 帧间隙：Impact Knockdown `OnCompleted` → `ChainGetUp()` 发生在 `CheckCompletion()` 中，新请求下帧才仲裁。视觉影响极小（1/60s）— P2
- [ ] 霸体判定未实现：有 ImpactEffect 即受击，不比较 staggerValue vs 自身霸体值 — P1 后续接 PropertyTable
- [ ] 死亡系统未实现：HP≤0 仅播放倒地动画不动，无 Ragdoll / DeathEvent / 复活 — P0 后续 task
- [ ] `WorldToLocalDirection` 假设角色 forward=Z+，模型旋转偏移时方向偏差 — P2

## Cross-References

### Related Plans
- [../plans/dapper-inventing-parnas.md](../plans/dapper-inventing-parnas.md) — 本 session 的实施计划

### Related Tech Docs
- `tech/L2-services/L2-modules/L3-ability/` — SDamageInfo, ImpactEffectSO, EHitReactionLevel
- `tech/L2-services/L2-modules/L3-character/animation/` — AnimationBrain, DriverArbiter, HitReactionDriver
- `tech/L2-services/L2-modules/L3-character/combat/` — CharacterCombat

### Related Design Docs
_None — 受击系统暂无独立设计文档。_

### Flag for Design Doc Creation
- [ ] NEW design doc needed for: `combat/hit-reaction` — 受击反应系统有玩家可见行为变化（受击硬直、击倒起身），涉及数值设计（staggerValue）、交互规则（霸体抢占），需要策划可见文档
