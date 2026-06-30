# 短期开发计划

> 更新: 2026-06-30
> 分支: `feature/ability-pipeline`
> 原则: 每步有可玩增量，先完成基础设施再铺玩法
> 前置: Character 模块重构 ✅ · Properties 系统 ✅ · Animation 重构 ✅ · Ability 数据资产 ✅ · AbilityTreeSO ✅ · EntityService + Container ✅ · Tag 6 域 339 标签 ✅ · Equipment→技能闭环 ✅ · PropertyTree Equipment 层重构 ✅

---

## 近期完成（2026-06）

| 事项 | 说明 |
|------|------|
| Properties 系统全量落地 | 8 类型 / ~185 PropertyDef / 30 Trees / Editor 完整 |
| Properties 接管角色物理 | `CharacterPhysique` + `GroundSystemConfigSO` 替代 3 个旧 SO |
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

## S1 — Properties 深度接入 [~1天]

> 背景: 物理属性已进 Properties，但速度系数层仍是占位。
> `Stance.motionSpeedScale = 1f` 硬编码，姿势/负重/敏捷不参与速度计算。
> S2/S3 不依赖 S1，可并行推进。

| # | 任务 | 涉及 |
|---|------|------|
| S1.1 | Properties 定义补充 — `Movement/SpeedCrouch`, `Movement/SpeedCrawl`, `Attributes/Agility` | `properties_all.json` |
| S1.2 | `Stance` — `motionSpeedScale` 接入 `Physique`，条件守卫恢复 | `Stance.cs` |
| S1.3 | `GroundLocomotion` — posture-aware speed | `GroundLocomotion.cs` |
| S1.4 | `CharacterPhysique` — 追加 Agility / SpeedCoefficient | `CharacterPhysique.cs` |
| S1.5 | `BaseMovingState` — 动画速度接入姿势系数 | `BaseMovingState.cs` |

**可验证增量**: 负重变化影响移速，冲刺/匍匐速度差异化。

---

## S2 — 装备→技能闭环 [✅ 完成]

> **目标**: 最小闭环——角色持有装备 → 切换装备触发技能切换 → 释放技能 → 扣血。
> 不依赖 AbilityComponent/HitReactionComponent，直接通过现有 AbilityExecutor 跑通全链路。
>
> 设计文档: [ability-forest.md](../tech/L2-services/L2-modules/L3-ability/ability-forest.md)

```
数据流: Entity → Container → CharacterEquipment.SyncEquipment → GripTags
          → AbilityForest.SetWeaponTags() → ResolvedActives[]
          → PlayerDirector → AbilityExecutor.TryActivate() → ②③④⑤ → 扣血 ❌
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
| S2.8 | **`AbilityExecutor` 伤害管线** | ❌ 未做 | DamageEffectSO → SDamageInfo → AbilityReactor.Resolve 全部注释。**内含进 S3.1** |

**⚡ 装备→技能→输入→②③④ 全链路已跑通**，唯 ⑤→⑥ 伤害结算未接通。
S2.8 不在行将废弃的 AbilityExecutor 上修修补补——内含进 S3.1 AbilityComponent 正编，一步到位读武器 PropertyTree ATK。

---

## S3 — Ability Pipeline 运行时 [~4天] 🚧 施工中

> 背景: S2 闭环用现有 AbilityExecutor 直接调用。S3 将管道正式化——AbilityComponent（发送中枢）+ HitReactionComponent（接收中枢）+ AbilityDriver（动画驱动）。
> 设计文档: [ability-pipeline-design.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md)

**架构**: AbilityComponent（发送中枢 → ②③④⑤）→ HitReactionComponent（接收中枢 → ⑥⑦⑧）

| # | 任务 | 说明 | 状态 |
|---|------|------|------|
| S3.1 | **`AbilityComponent`** + `IConditionModifier` + ⑧ 广播 | `TryActivate()` + 被动触发 + 冷却/OwnedTags + 门控回调 + 事件发布 | 🚧 StateMachine 框架完成 |
| S3.2 | **`HitReactionComponent`** | `Resolve(SResolvedHit[])` + `ReceiveRawDamage()`。三阶段：Avoidance → Mitigation → Absorption | ⏳ 未开始 |
| S3.3 | **`AbilityDriver`** | ③ 释放动画 — 继承 `BaseCharacterAnimationDriver`，消费 `AbilityActivationSO` Windup→Fire→Recovery | ⏳ 未开始 |
| S3.4 | **CharacterActor 集成 + 闭环测试** | AbilityForest 对接 AbilityComponent；Q 键全链路验证 | ⏳ 未开始 |

### S3.1 已完成

- `IState<TContext>` + `StateMachine<TContext>` 泛型基础设施（零领域依赖，[MARK] 可提至 Shared/）
- `ActiveAbilityPipeline` — 持有 `StateMachine<SActiveAbilityContext>`，`Start()` / `Tick()` / `Interrupt()`
- `AbilityExecutor` 重构 — 旧代码归档 `#region OLD`，新增 `Queue<SQueuedSkill>` + `Enqueue()` 队列接口
- `GatingState` ② 门控落地 — 冷却/互斥/外部条件三闸门
- `CostState` ③ 资源消耗 — 双阶段预检+扣除
- `PlayerDirector` 对接 — `TryActivate` → `Enqueue`，武器 Entity 传入管道

### S3.1 待完成

- `ExecutionState` ④⑤ — 搜索命中 + 效果载荷（依赖旧 `AbilitySearch`/`AbilityEffects` 迁入）
- `CooldownState` — 冷却施加
- `RecoveryState` — 等待后摇结束
- `AbilityExecutor` 旧 `#region` 清理

**可验证增量**: 按 Q → AbilityComponent.TryActivate → 门控 → AbilityDriver 播放横斩动画 → 搜索命中 → HitReactionComponent 结算扣血 → 事件广播。

---

## S4 — Combat 管线补完 [~1天]

> `CharacterCombat.cs` 已有骨架，三个核心判定待实现。

| # | 任务 | 涉及 |
|---|------|------|
| S4.1 | 施展方属性修正 — 力量/穿透 (`IEffectModifier`) | `CharacterCombat.OnEffectModify` |
| S4.2 | 回避判定 — 闪避率 + 短路 | `CharacterCombat.OnResolveDamage` |
| S4.3 | 吸收结算 — 护盾伤害吸收 | `CharacterCombat.OnResolveDamage` |
| S4.4 | 字符串路径 → Properties 路径常量 | `CharacterCombat.cs` |

---

## S5 — 动画系统补完 [~3天]

> S5.2 替代旧方案：当前头部朝向通过 `Vector2MixerState` 混合动画 Pose 实现，
> 改为安装 Unity Animation Rigging 包，用 `MultiAimConstraint` + `RigBuilder` 直接驱动头骨 IK 朝向目标点。

| # | 任务 | 说明 | 耗时 |
|---|------|------|------|
| S5.1 | Footstep 事件桥接 | `BaseLayer.FootstepCallback → AnimationBrain.OnFootstep` | ~0.5天 |
| S5.2 | **Head Look IK** — 代码驱动头部转向 | 安装 Animation Rigging 包 + 新建 `HeadLookIK` + 移除旧 headLookMixer | ~1.5天 |
| S5.3 | Crawl 动画 mixer | `BaseMovingState` + `LocomotionAnimationSetSO` + `crawlMixer` | ~0.5天 |
| S5.4 | AirLand 分级落地 | Gait 参数混合 `landLight/landHard` LinearMixer | ~0.5天 |
| S5.5 | Traversal 动画迁移 | `TraversalDriver` → `TraversalAnimationSetSO` | ~0.5天 |

---

## 优先级依赖

```
S1 (~1天) ──────────────────────────────┐
                                         │
S2 (✅ 基础闭环完成) ────────────────────┤
                                         ├── S5 (~3天 — 可并行)
S3 (~4天 — Ability Pipeline，当前焦点) ──┤
                                         │
S4 (~1天 — Combat 补完) ────────────────┘
```

**S2→S3 无阻塞依赖。S3 可直接开工。**

S2.8（伤害管线）内含进 S3.1 — AbilityComponent 正编时直接读武器 PropertyTree ATK。
S1 优先于 S4 但 S3 不依赖 S1。

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
