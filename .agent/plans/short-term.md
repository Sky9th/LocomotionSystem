# 短期开发计划

> 更新: 2026-06-25
> 分支: `feature/ability-pipeline`
> 原则: 每步有可玩增量，先完成基础设施再铺玩法
> 前置: Character 模块重构 ✅ · Properties 系统 ✅ · Animation 重构 ✅ · Ability 数据资产 ✅ · AbilityTreeSO ✅

---

## 近期完成（2026-06 上半月）

| 事项 | 说明 |
|------|------|
| Properties 系统全量落地 | 8 类型 / ~185 PropertyDef / 30 Trees / Editor 完整 |
| Properties 接管角色物理 | `CharacterPhysique` + `GroundSystemConfigSO` 替代 3 个旧 SO |
| Module 系统 + ctx 全链路 | BaseService 删除，Service 直接继承 ModuleComponent |
| Animation 重构 | LinearMixer + In-line Transition + 废弃 State 清理 |
| Ability 数据资产 | Search / Activation / Effect / Noise / Passive 全量 SO + Editor |
| Tag 系统 | 199 全量 GameplayTag + EditorTreeView 迁移 |
| CharacterCombat 骨架 | 回调绑定 + 伤害管线骨架 |
| 武器模型 | PolygonApocalypse 武器模型 + 材质导入 |
| AbilityTreeSO | 代码 + Import/Export + 7 技能 + 3 天生树 + 9 Tag |

---

## S1 — Properties 深度接入 [~1天]

> 背景: 物理属性已进 Properties，但速度系数层仍是占位。
> `Stance.motionSpeedScale = 1f` 硬编码，姿势/负重/敏捷不参与速度计算。

| # | 任务 | 涉及 |
|---|------|------|
| S1.1 | Properties 定义补充 — `Movement/SpeedCrouch`, `Movement/SpeedCrawl`, `Attributes/Agility` | `properties_all.json` |
| S1.2 | `Stance` — `motionSpeedScale` 接入 `Physique`，条件守卫恢复 | `Stance.cs` |
| S1.3 | `GroundLocomotion` — posture-aware speed | `GroundLocomotion.cs` |
| S1.4 | `CharacterPhysique` — 追加 Agility / SpeedCoefficient | `CharacterPhysique.cs` |
| S1.5 | `BaseMovingState` — 动画速度接入姿势系数 | `BaseMovingState.cs` |

**可验证增量**: 负重变化影响移速，冲刺/匍匐速度差异化。

---

## S2 — 装备→技能闭环 [~4天]

> **目标**: 最小闭环——角色持有装备 → 切换装备触发技能切换 → 释放技能 → 扣血。
> 不依赖 AbilityComponent/HitReactionComponent，直接通过现有 AbilityExecutor 跑通全链路。
>
> 设计文档: [ability-forest.md](../tech/L2-services/L2-modules/L3-ability/ability-forest.md)

```
数据流: ItemInstance → Container<T> → CharacterActor.SwitchWeapon
          → AbilityForest.Resolve() → ctx.AbilitySlots
          → PlayerDirector → AbilityExecutor.TryActivate() → 扣血
```

| # | 任务 | 说明 | 耗时 |
|---|------|------|------|
| S2.1 | **`ItemInstance.cs`** | 纯 C# 类：Id(GUID) + Def + Props + Count + Tick。`static Create(ItemDefSO)` 工厂方法 | ~0.5天 |
| S2.2 | **`Container.cs`** | 泛型 `Container<T>` + `ContainerSlot`。Place/Remove/CanAccept/FindSlotFor | ~1天 |
| S2.3 | **`AbilityForest.cs`** | 纯 C# 类：多来源活跃树集合。AddTree/RemoveBySource/Resolve。树 ∩ 武器Tag → 技能槽 + 被动列表 | ~0.5天 |
| S2.4 | **`ItemDefSO` 扩展** | 新增 `GrantedAbilityTrees` 临时 C# 字段（远期进 PropertyTree） | ~0.2天 |
| S2.5 | **`CharacterBuildContext` 改造** | SkillSlot1/2 → `AbilitySlots[]` + `ActivePassives[]` | ~0.3天 |
| S2.6 | **`CharacterActor` 改造** | 创建 Container(RightHand/LeftHand) + AbilityForest + SwitchWeapon 方法 | ~0.5天 |
| S2.7 | **`PlayerDirector` 改造** | ProcessEquipInput → SwitchWeapon → 消费 ctx.AbilitySlots | ~0.5天 |
| S2.8 | **`AbilityExecutor` 取消注释** | baseDamage 从武器 PropertyTree 读取 ATK，伤害管线取消注释 | ~0.5天 |

**依赖链**: S2.1 → S2.2 → S2.3 → S2.4 → S2.5 → S2.6 → S2.7 → S2.8（串行）

**可验证增量**: 按 1 装备刀 → Q 技能栏显示斩击；按 2 装备手枪 → Q 技能栏切换为射击；按 Q → 搜索目标 → 扣血 → HUD 显示。

---

## S3 — Ability Pipeline 运行时 [~4天]

> 背景: S2 闭环用现有 AbilityExecutor 直接调用。S3 将管道正式化——AbilityComponent（发送中枢）+ HitReactionComponent（接收中枢）+ AbilityDriver（动画驱动）。
> 设计文档: [ability-pipeline-design.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md)

**架构**: AbilityComponent（发送中枢 → ②③④⑤）→ HitReactionComponent（接收中枢 → ⑥⑦⑧）

| # | 任务 | 说明 | 耗时 |
|---|------|------|------|
| S3.1 | **`AbilityComponent`** + `IConditionModifier` + ⑧ 广播 | `TryActivate()` + 被动触发 + 冷却/OwnedTags + 门控回调 + 事件发布 | ~1.5天 |
| S3.2 | **`HitReactionComponent`** | `Resolve(SResolvedHit[])` + `ReceiveRawDamage()`。三阶段：Avoidance → Mitigation → Absorption | ~1天 |
| S3.3 | **`AbilityDriver`** | ③ 释放动画 — 继承 `BaseCharacterAnimationDriver`，消费 `AbilityActivationSO` Windup→Fire→Recovery | ~0.5天 |
| S3.4 | **CharacterActor 集成 + 闭环测试** | AbilityForest 对接 AbilityComponent；Q 键全链路验证 | ~1天 |

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
S2 (~4天 — 装备→技能闭环，当前核心) ─────┤
                                         ├── S5 (~3天 — 可并行)
S3 (~4天 — Ability Pipeline) ────────────┤
                                         │
S4 (~1天 — Combat 补完) ────────────────┘
```

**总计约 10 天（2 周）。基于 [施工历史](../plans/long-term.md#施工历史用于工期校准) 校准。**

S2 是当前分支 `feature/ability-pipeline` 的第一目标——先让装备→技能→伤害跑通。
S3 在 S2 基础上把管道正式化，S3.4 做集成。
S1 优先于 S2 — AbilityForest 需要读 Properties（属性门槛—远期）。
S5 可与 S2-S4 并行推进。

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
