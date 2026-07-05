# 短期开发计划

> 更新: 2026-07-05
> 分支: `feature/ability-pipeline`
> 原则: 每步有可玩增量，先完成基础设施再铺玩法
> 前置: Character 模块重构 ✅ · Properties 系统 ✅ · Animation 重构 ✅ · Ability 数据资产 ✅ · AbilityTreeSO ✅ · EntityService + Container ✅ · Tag 6 域 339 标签 ✅ · Equipment→技能闭环 ✅ · PropertyTree Equipment 层重构 ✅ · Ability Pipeline 8 State 全就位 ✅

---

## 近期完成（2026-06）

| 事项 | 说明 |
|------|------|
| Properties 系统全量落地 | 8 类型 / ~185 PropertyDef / 30 Trees / Editor 完整 |
| Properties 接管角色物理 | PropertyTable + `GroundSystemConfigSO` 替代 3 个旧 SO（v0.36.11 Physique 已删除） |
| Module 系统 + ctx 全链路 | BaseService 删除，Service 直接继承 ModuleComponent |
| Animation 重构 | LinearMixer + In-line Transition + 废弃 State 清理 |
| Ability 数据资产 | Search / Activation / Effect / Noise / Passive 全量 SO + Editor |
| AbilityTreeSO | 代码 + Import/Export + 7 技能 + 3 天生树 + 9 Tag |
| Event 系统统一 | `GameEvent<T>` 唯一推模式通道 |
| EntityService + Entity 数据模型 | Id + Preset + Properties + StackCount + Tick + NestedContainer |
| Container 系统 | 泛型 `Container<T>` + Place/Remove/CanAccept/FindSlotFor + 嵌套递归 |
| AbilityForest | 多来源树集合 + Tag 兼容过滤 → ResolvedActives[] |
| Entity→CharacterActor 数据管线 | Slots PropertyTree → BodyContainer + CharacterEquipment GO 同步 |
| Tag 架构重构 | 6 域 339 标签（Ability/Identity/Body/Entity/Grip/Noise）+ GameplayTag→rTag 全局改名 |
| CharacterCombat 骨架 | 回调绑定 + 伤害管线骨架 |
| 武器模型 | PolygonApocalypse 武器模型 + 材质导入 |
	
	---
	
	## 近期完成（2026-07）
	
	| 事项 | 说明 |
	|------|------|
	| 受击反应管线 | SDamageInfo.ImpactEffect → ExecutionState → CharacterCombat → DriverArbiter → HitReactionDriver 全链路 |
	| ImpactEffectSO + EHitReactionLevel | 资产驱动受击等级（Flinch/Stagger/Knockdown）+ 策划直接配置 |
	| HitReactionDriver | 受击动画驱动 — 播放 MixerTransition2D + blend parameter + FadeIn 临时覆写 |
	| DriverArbiter 抢占规则 | H1 idle→接受；H2 HitReaction 抢占一切；Traversal↔Ability 互斥 |
	| CharacterCombat 受击+死亡 | OnReaction（资产驱动+Knockdown起身链）+ OnDamaged（HP≤0倒地不起） |
	| 受击动画数据层 | LocomotionAnimationSetSO 4 个 hitReaction 字段 + ImportExport 序列化（v0.36.9） |
| 伤害飘字系统 | DamageNumberOverlay + DamageNumberWidget — HP 变化时弹出浮动伤害数字 |
| Ability Pipeline 8 State 全就位 | Gating→Cost→Windup→Cooldown→Execution→Recovery→Completed/Rejected，ref TContext 零拷贝 |
| AbilityDriver ③ 释放动画 | 消费 AbilityActivationSO，Windup→Fire Animancer 事件注入 + animationSpeed 调速 |
| **S1 Properties 深度接入** | 删除 Physique 缓存，GroundLocomotion 实现 motionSpeedScale 公式（Agility + CarryWeight）|
| **Physique 删除** | 8 字段 struct 全量替换为 PropertyTable.GetFloat 按需读取，全代码库统一 |

---

## S1 — Properties 深度接入 [✅ 完成]

> **实际实施**（2026-07-05）：
> - 删除 `CharacterPhysique.cs`（8 字段缓存 struct），全量替换为 `PropertyTable.GetFloat(PropertyPath.X)` 按需读取
> - `GroundLocomotion` 实现 `ComputeMotionSpeedScale()` 公式：`1 + Agility × agilitySpeedBonus − WeightPenalty`
> - 公式系数由 `GroundSystemConfigSO.agilitySpeedBonus(0.03)` / `weightPenaltyRatio(0.2)` 全局配置驱动
> - `Stance` 移除硬编码 `1f`，接受外部传入的 `motionSpeedScale`
> - `BaseMovingState` blend 参数同步缩放
> - `CharacterConst` 新增 `Agility`、`CarryWeight` 常量
> - 姿势系数不参与速度计算——动画系统已通过 `GetNativeSpeed(gait)` 编码姿势差异

**可验证增量**: 负重变化影响移速（Container 有物品时 motionSpeedScale < 1.0），敏捷属性生效（默认 Agility=5 → 1.15x speed）。

---

## S2 — 装备→技能闭环 [✅ 完成]

> **目标**: 最小闭环——角色持有装备 → 切换装备触发技能切换 → 释放技能 → 扣血。
> 不依赖 AbilityComponent/HitReactionComponent，直接通过现有 AbilityExecutor 跑通全链路。
>
> 设计文档: [ability-forest.md](../tech/L2-services/L2-modules/L3-ability/ability-forest.md)

```
数据流: Entity → Container → CharacterEquipment.SyncEquipment → GripTags
          → AbilityForest.SetWeaponTags() → ResolvedActives[]
          → PlayerDirector → AbilityExecutor.TryActivate() → ②③④⑤⑥⑦⑧ ✅
```

| # | 任务 | 状态 | 说明 |
|---|------|------|------|
| S2.1 | **`ItemInstance.cs`** | ~~过时~~ | `Entity` 已全覆盖（Id/Preset/Properties/StackCount/Tick），不需另建类 |
| S2.2 | **`Container.cs`** | ✅ 完成 | 泛型 `Container<T>` + Place/Remove/CanAccept/FindSlotFor |
| S2.3 | **`AbilityForest.cs`** | ✅ 完成 | AddTree/RemoveBySource/Resolve → ResolvedActives[]/ResolvedPassives[] |
| S2.4 | **`ItemDefSO` 扩展** | ~~过时~~ | 技能树来源是学习/天生，武器只做 Tag 过滤——不授予技能树 |
| S2.5 | **`CharacterBuildContext` 改造** | ~~框架覆盖~~ | `AbilityForest.ResolvedActives[]` 直接提供，PlayerDirector 已消费 |
| S2.6 | **`CharacterActor` 改造** | ⚠️ 变通 | 无 SwitchWeapon 方法，`CharacterEquipment.SyncEquipment()` 每帧 diff 等效实现 |
| S2.7 | **`PlayerDirector` 改造** | ⚠️ Hack | 硬编码 EquipMap + 裸操作 Container，功能可用，远期装备栏 UI 清理 |
| S2.8 | **`AbilityExecutor` 伤害管线** | ✅ 完成 | 已内含进 S3 — ExecutionState → AbilityReactor.Resolve → CharacterCombat → HP 写入 |

**⚡ 装备→技能→输入→②③④⑤⑥⑦⑧ 全链路已跑通** ✅。
伤害结算已在 S3 Pipeline 正编落地——ExecutionState 构造 SDamageInfo → AbilityReactor.Resolve → CharacterCombat 回调 → HP 写入 + 伤害飘字 + 受击反应动画。

---

## S3 — Ability Pipeline 运行时 [~2天剩余]

> 背景: S2 闭环用现有 AbilityExecutor 直接调用。S3 将管道正式化为 8 State 状态机（Gating → Cost → Windup → Cooldown → Execution → Recovery）。
> 实现文档: [ability-pipeline-states.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-states.md)
> 设计概念: [ability-pipeline-design.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md) ⛔ DEPRECATED（概念保留，实现细节过时）

**架构**: AbilityExecutor（发送中枢 → ②③④⑤）→ AbilityReactor（接收中枢 → ⑥⑦⑧）→ CharacterCombat（修改器回调桥接）

| # | 任务 | 说明 | 状态 |
|---|------|------|------|
| S3.1 | **`AbilityExecutor` + 8 State 管线** | `TryActivate()` + `Enqueue()` + StateMachine 全链路 ②-⑧ | ✅ 完成 |
| S3.2 | **`AbilityReactor` + `CharacterCombat`** | `Resolve(SDamageInfo)` + 回调桥接（Effect/Resolution/ApplyDamage/Reaction/OnDamaged） | ✅ 框架完成 |
| S3.3 | **`AbilityDriver`** | ③ 释放动画 — 继承 `BaseAnimationDriver`，消费 `AbilityActivationSO` Windup→Fire→Recovery | ✅ 完成 |
| S3.4 | **伤害飘字** | `DamageNumberOverlay` + `DamageNumberWidget` — HP 变化时弹出浮动数字 | ✅ 完成 |
| S3.5 | **闭环测试 + 旧代码清理** | Q 键全链路验证；AbilityExecutor 旧 `#region OLD` 清理 | ⏳ 待做 |

### S3.1 已完成（全 8 State）

- `IState<TContext>` + `StateMachine<TContext>` 泛型基础设施（零领域依赖，[MARK] 可提至 Shared/）
- `ActiveAbilityPipeline` — 持有 `StateMachine<SActiveAbilityContext>`，`Start()` / `Tick()` / `Interrupt()`
- `AbilityExecutor` — `Queue<SQueuedSkill>` + `Enqueue()` 队列接口
- `GatingState` ② 门控落地 — 冷却/互斥/外部条件三闸门
- `CostState` ③ 资源消耗 — 双阶段预检+扣除（PropertyTable 内建路径 + 回调兜底）
- `WindupState` ③ 前摇计时 — windupDuration / animationSpeed + canCancelWindup 霸体控制
- `CooldownState` 冷却施加 — 独立冷却 + 联动冷却 + MinCooldown=0.05s 防连发
- `ExecutionState` ④⑤ 落地 — Fire 帧物理查询（Cone/Ray/Circle 内联）+ BuildDamageInfo + EffectCallback 修正 + 逐 hit Reactor.Resolve
- `RecoveryState` ③ 后摇 — recoveryDuration / animationSpeed + canCancelRecovery 霸体控制 + 动画完成检测
- `TerminalStates` — Idle / Completed / Rejected 终态
- `PlayerDirector` 对接 — `TryActivate` → `Enqueue`，武器 Entity 传入管道
- `SDamageInfo` — `ImpactEffect` 字段，双构造函数向后兼容

### S3.2 已完成

- `AbilityReactor` — `Resolve(SDamageInfo)` + 5 回调委托（Resolution / ApplyDamage / Reaction / OnDamaged / 事件发布）
- `CharacterCombat.OnWire()` — 接线 5 回调：`EffectCallback` / `ResolutionCallback` / `ApplyDamageCallback` / `ReactionCallback` / `OnDamagedCallback`
- `CharacterCombat.OnReaction()` — ImpactEffectSO 资产驱动受击等级 → AnimationRequest → HitReactionDriver
- `CharacterCombat.OnDamaged()` — HP≤0 倒地不起（无 OnCompleted 起身链）
- `CharacterCombat.OnResolveDamage()` — 基础 Mitigation（Endurance 减免），Avoidance/Absorption 占位
- `CharacterCombat.OnApplyDamage()` — HP 直接写入 + 日志

### S3.5 待完成

- **闭环测试**: 按 Q → AbilityExecutor.TryActivate → 8 State 全链路 → AbilityReactor.Resolve → CharacterCombat → 伤害飘字
- **旧代码清理**: AbilityExecutor 旧 `#region OLD_IMPLEMENTATION` 删除（TryActivate、ExecutePassive、OnTriggerEnter/Exit）；废弃文件 AbilityEffects.cs、SearchState.cs 删除
- **被动技能物理回调迁移**: OnTriggerEnter/Exit 当前仍用 `runtimePassives`，未接入 InstanceManager → 迁移至 InstanceManager 统一管理
- **AbilityForest 接入 InstanceManager**: SyncInstances 调用链打通，AbilityForest 从 InstanceManager 获取实例列表同步

**可验证增量**: 按 Q → 门控 → AbilityDriver 播放横斩动画 → Fire 帧物理查询 → 命中结算扣血 → 伤害飘字 → 受击反应动画。

---

## S4 — Combat 管线补完 [~1天]

> `CharacterCombat.cs` 已有骨架，⑤/⑥ 修改器公式当前为占位/TODO。S3.5 闭环测试通过后开工。
> 回避/护盾/霸体依赖其他系统（闪避率、护盾值、霸体值），延后到对应系统就位后再处理。

| # | 任务 | 涉及 | 状态 |
|---|------|------|------|
| S4.1 | 施展方属性修正 — 力量/穿透 (`IEffectModifier`) | `CharacterCombat.OnEffectModify` | ⏳ |
| S4.2 | 字符串路径 → Properties 路径常量 | `CharacterCombat.cs` | ⏳ |
| S4.3 | **Reactor→Caster OnHit 通知通路** — Exe 侧知道命中是否完成，Caster→Reactor 反馈 | `AbilityReactor` / `AbilityExecutor` | ⏳ |
| S4.4 | **SDamageInfo 职责明确** — Exe 侧算伤害还是交给 Reactor 算？定论后统一 | `SDamageInfo` / `ExecutionState` | ⏳ |
| S4.5 | **Self-damage Amount=0** — 血魔法等技能的自我伤害公式 | `AbilityReactor.Resolve` | ⏳ |

---

## S5 — 动画系统补完 [~2天]

> S5.0（受击动画管线）已完成。S5.2 替代旧方案：当前头部朝向通过 `Vector2MixerState` 混合动画 Pose 实现，
> 改为安装 Unity Animation Rigging 包，用 `MultiAimConstraint` + `RigBuilder` 直接驱动头骨 IK 朝向目标点。

| # | 任务 | 说明 | 耗时 | 状态 |
|---|------|------|------|------|
| S5.0 | **受击动画管线** | HitReactionDriver + DriverArbiter 抢占 + LocomotionAnimationSetSO hitReaction 字段 | ~1天 | ✅ 完成 |
| S5.1 | Footstep 事件桥接 | `BaseLayer.FootstepCallback → AnimationBrain.OnFootstep` | ~0.5天 | ⏳ |
| S5.2 | **Head Look IK** | 安装 Animation Rigging 包 + 新建 `HeadLookIK` + 移除旧 headLookMixer | ~1天 | ⏳ |
| S5.3 | Crawl 动画 mixer | `BaseMovingState` + `LocomotionAnimationSetSO` + `crawlMixer` | ~0.5天 | ⏳ |
| S5.4 | AirLand 分级落地 | Gait 参数混合 `landLight/landHard` LinearMixer | ~0.5天 | ⏳ |
| S5.5 | Traversal 动画迁移 | `TraversalDriver` → `TraversalAnimationSetSO` | ~0.5天 | ⏳ |

---

## 低优先级 / 技术债

> 不阻塞当前里程碑，在 S3-S5 施工中顺手修复或远期处理。

| # | 事项 | 说明 | 阻塞原因 |
|---|------|------|----------|
| L1 | **Avoidance/Mitigation/Absorption 三阶段拆分** | 替换单 ResolutionCallback 为三段管线 | 回避/护盾系统未就位 |
| L2 | **回避判定** — 闪避率 + 短路 | `CharacterCombat.OnResolveDamage` | 闪避属性/装备系统 |
| L3 | **吸收结算** — 护盾伤害吸收 | `CharacterCombat.OnResolveDamage` | 护盾系统未设计 |
| L4 | **霸体阈值判定** — staggerValue vs 自身霸体值 | `CharacterCombat.OnReaction` | 霸体值属性体系 |
| L5 | **ComputeDamage 交叉乘积按 element tag 匹配** | 伤害计算中元素类型交叉匹配逻辑 | — |
| L6 | **RangedWeaponSO 临时 SO 泄漏** | `ScriptableObject.CreateInstance` 未销毁 | — |
| L7 | **AddBuffTags 默认 owner=null** | 潜在 footgun，调用方容易漏传 owner | — |
| L8 | **Reactor ApplyEffects 确认 public/private** | 明确 API 边界 | — |
| L9 | **伤害类型转换** | 防弹衣穿刺→钝伤等类型映射 | 防弹衣系统未就位 |

---

## 优先级依赖

```
S3.5 (~1天 — 闭环测试 + 旧代码清理 + InstanceManager 集成) ──── 当前焦点
    │
    ├── S4 (~1天 — Combat 补完) ──── 下一站
    │
    └── S5 (~2天 — 动画补完, S5.0 ✅) ──── 可并行

低优先级 L1-L5 ──── 远期 / 顺手修复
S1 ✅ 完成（2026-07-05）
```

**S3.5 → S4 直接依赖**：S4 的 OnEffectModify/OnResolveDamage 公式验证依赖闭环测试跑通。S3.5 被动迁移 + InstanceManager 集成为后续三阶段拆分打底。

---

## 不纳入短期计划

- 资源系统 / 背包 / 负重（Phase 5）
- 建造基础（Phase 6）
- 时间日夜（Phase 7）
- 农业烹饪 / NPC / 尸潮 / 科技树（Phase 8-11）
- 扩展打磨 — 连招 / 投射物 / 噪音连锁 / 特殊感染者 / 丧尸化（Phase 12+）
- 角色创建 UI / 存档系统（SCharacterBuild 仅定义结构体，不使用）
- 技能槽溢出处理（actives > 4 排序）
- PropertyType.Struct（GrantedAbilityTrees 远期迁移）
