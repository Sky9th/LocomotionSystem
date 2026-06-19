# 短期开发计划

> 更新: 2026-06-19
> 分支: `feature/ability-pipeline`
> 原则: 每步有可玩增量，先完成基础设施再铺玩法
> 前置: Character 模块重构 ✅ · Properties 系统 ✅ · Animation 重构 ✅ · Ability 数据资产 ✅

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

---

## S1 — Properties 深度接入 [~1天]

> 背景: 物理属性已进 Properties，但速度系数层仍是占位。
> `Stance.motionSpeedScale = 1f` 硬编码，姿势/负重/敏捷不参与速度计算。
> 参照 6/19 Properties 接管物理（3 SO 删除 + 12 消费者 + 9 属性定义，1天），本次范围更小。

| # | 任务 | 涉及 |
|---|------|------|
| S1.1 | Properties 定义补充 — `Movement/SpeedCrouch`, `Movement/SpeedCrawl`, `Attributes/Agility` | `properties_all.json` |
| S1.2 | `Stance` — `motionSpeedScale` 接入 `Physique`，条件守卫恢复 | `Stance.cs` |
| S1.3 | `GroundLocomotion` — posture-aware speed: `animNativeSpeed × posture系数 × 负重惩罚` | `GroundLocomotion.cs` |
| S1.4 | `CharacterPhysique` — 追加 Agility / SpeedCoefficient 字段 | `CharacterPhysique.cs` |
| S1.5 | `BaseMovingState` — 动画速度接入姿势系数 | `BaseMovingState.cs` |

**可验证增量**: 负重变化影响移速，冲刺/匍匐速度差异化。

---

## S2 — Ability Pipeline 运行时 [~5天]

> 背景: [ability-pipeline-design.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md) 八维度管道设计完成，数据资产完备。当前仅 `AbilityExecutor` + `AbilityReactor` + `CharacterCombat` 有骨架代码。
> 三大核心组件待落地，最接近的历史参照是 6/16-17 Module 系统 + ctx 全链路（新建 4 类型 + 迁移 CharacterActor + 删除 BaseService，2天）。AbilityComponent 复杂度相当，外加 HitReactionComponent 和 AbilityDriver 各为一个独立组件。

**架构**: AbilityComponent（发送中枢 → ②③④⑤）→ HitReactionComponent（接收中枢 → ⑥⑦⑧）

| # | 任务 | 说明 | 耗时 |
|---|------|------|------|
| S2.1 | **`AbilityComponent`** + `IConditionModifier` + ⑧ 广播 | `TryActivate()` + 被动触发 + 冷却/OwnedTags + 门控回调 + 事件发布 | ~2天 |
| S2.2 | **`HitReactionComponent`** | `Resolve(SResolvedHit[])` + `ReceiveRawDamage()`。三阶段：Avoidance → Mitigation → Absorption | ~1.5天 |
| S2.3 | **`AbilityDriver`** | ③ 释放动画 — 继承 `BaseCharacterAnimationDriver`，消费 `AbilityActivationSO` Windup→Fire→Recovery | ~1天 |
| S2.4 | **CharacterActor 集成 + 闭环测试** | 双向绑定 + 替换临时槽位 + Q 键全链路验证 | ~0.5天 |

**可验证增量**: 按 Q → AbilityComponent.TryActivate → 门控 → AbilityDriver 播放横斩动画 → 搜索命中 → HitReactionComponent 结算扣血 → 事件广播。

---

## S3 — Combat 管线补完 [~1天]

> `CharacterCombat.cs` 已有骨架，三个核心判定待实现。单文件补逻辑，参照 6/11 Cost 标签体系（1天）。

| # | 任务 | 涉及 |
|---|------|------|
| S3.1 | 施展方属性修正 — 力量/穿透 (`IEffectModifier`) | `CharacterCombat.OnEffectModify` |
| S3.2 | 回避判定 — 闪避率 + 短路 | `CharacterCombat.OnResolveDamage` |
| S3.3 | 吸收结算 — 护盾伤害吸收 | `CharacterCombat.OnResolveDamage` |
| S3.4 | 字符串路径 → Properties 路径常量 | `CharacterCombat.cs` |

---

## S4 — 动画系统补完 [~3天]

> S4.2 替代旧方案：当前头部朝向通过 `Vector2MixerState` 混合动画 Pose 实现（`AnimationBrain.headLookMixer`），
> 改为安装 Unity Animation Rigging 包，用 `MultiAimConstraint` + `RigBuilder` 直接驱动头骨 IK 朝向目标点。
> 旧 `headLookMixer` / `headLookSmoothingSpeed` / `LookMixer` TODO 全部移除。
> S4.2 是新包接入 + 新建组件，参照 6/13 FloatAdjunct+BuffEffectSO（新建底座，1天），IK 需额外适配时间。

| # | 任务 | 说明 | 耗时 |
|---|------|------|------|
| S4.1 | Footstep 事件桥接 | `BaseLayer.FootstepCallback → AnimationBrain.OnFootstep` | ~0.5天 |
| S4.2 | **Head Look IK** — 代码驱动头部转向，替代动画 `Vector2MixerState` | 安装 Animation Rigging 包 + 新建 `HeadLookIK` + 移除旧 headLookMixer | ~1.5天 |
| S4.3 | Crawl 动画 mixer | `BaseMovingState` + `LocomotionAnimationSetSO` + `crawlMixer` | ~0.5天 |
| S4.4 | AirLand 分级落地 | Gait 参数混合 `landLight/landHard` LinearMixer | ~0.5天 |
| S4.5 | Traversal 动画迁移 | `TraversalDriver` → `TraversalAnimationSetSO` | ~0.5天 |

---

## 优先级依赖

```
S1 (~1天)                    S4 (~3天 — 可并行)
        │                          │
        └──────┬───────────────────┘
               ▼
        S2 (~5天)
               │
               ▼
        S3 (~1天)
               │
               ▼
         长期 Phase 5 (资源系统)
```

**总计约 10 天（2 周）。** 基于 [施工历史](../plans/long-term.md#施工历史用于工期校准) 校准。

S1 + S4 可并行推进，先 S1 再 S4 串行也不影响总工期。
S2 是当前分支 `feature/ability-pipeline` 的核心目标。
S1 优先于 S2 — AbilityComponent 需要读 Properties（资源门控、属性修正）。

---

## 不纳入短期计划

以下已移入 [长期计划](long-term.md)：
- 资源系统 / 背包 / 负重（Phase 5）
- 建造基础（Phase 6）
- 时间日夜（Phase 7）
- 农业烹饪 / NPC / 尸潮 / 科技树（Phase 8-11）
- 扩展打磨 — 连招 / 投射物 / 噪音连锁 / 特殊感染者 / 丧尸化（Phase 12+）
