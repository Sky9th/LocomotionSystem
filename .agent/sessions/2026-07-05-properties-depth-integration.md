# 2026-07-05 — S1 Properties 深度接入 + Physique 删除

## Background

角色移动速度管线存在唯一硬编码瓶颈：`Stance.cs:28` 的 `motionSpeedScale = 1f`。所有步态速度直接取自动画资产，Agility（敏捷）和 CarryWeight（负重上限）虽已在 `properties_all.json` 中定义，但无代码消费。

`CharacterPhysique` struct 将 9 个属性缓存在 Start 快照中，绕过 PropertyTable 的 Modifier/Adjunct 系统。全代码库仅此一处使用缓存模式——其余 7 个消费者（CharacterCombat、CostState、Container 等）均按需读取。该 struct 注释自述「临时方案」，且 Height 字段写入后从未消费。

这是短期计划 S1 的内容，在 S3 Pipeline 完成后启动。

## Changes

### Physique 删除
- 删除 `CharacterPhysique.cs` + `.meta`（8 个消费字段，Height 死字段一并清除）
- `CharacterBuildContext` — 移除 `Physique` 属性 + 构造函数参数 + TODO 注释
- `CharacterActor` — 移除 `Physique = CharacterPhysique.From(Properties)` 创建行 + `physique: default` 参数
- `CharacterKinematic` — 8 处 `physique.X` 替换为 `props.GetFloat(PropertyPath.X)`
- `CharacterActor.Debug` — 2 处 `Physique.ObstacleMaxClimb` 替换为直接读

### 速度公式
- `GroundLocomotion` — 新增 `ComputeMotionSpeedScale()`，读取 Agility、CarryWeight、Acceleration 三个属性，计算 `motionSpeedScale = 1 + Agility × agilitySpeedBonus − WeightPenalty`。`desiredSpeed` 从 raw native speed 改为乘以系数
- `GroundSystemConfigSO` — 新增 `agilitySpeedBonus = 0.03f`、`weightPenaltyRatio = 0.2f` 字段（`[Header("Movement Formula")]`），全局共享资产调参
- `Stance` — `Evaluate()` 签名接收 `motionSpeedScale` 参数，移除硬编码 `1f`

### 动画同步
- `BaseMovingState` — blend 参数分母从 `rawNativeSpeed` 改为 `nativeSpeed × MotionSpeedScale`，动画混合树切换阈值与速度同步

### 常量 & 注释
- `CharacterConst` — Attributes 新增 `Agility`，Movement 新增 `CarryWeight`
- `LocomotionAnimationConfigSO` — 注释移除 Physique 引用

### 计划更新
- `short-term.md` — S1 标记完成，同步 S3 全 8 State 已就位、伤害飘字等近期完成项
- `long-term.md` — 受击管线等近期完成项补充

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 删除 Physique，改为按需读 | A: 在 Physique 上加 Agility/CarryWeight 字段 → 继续扩大快照问题，Buff/Debuff 后数据过期。B: 改 Physique 为 live accessor 包装 GetFloat → 无实质收益，多一层间接 | 全代码库 7/8 消费者已用按需读，保持一致。PropertyTable.GetFloat 成本 2 次 dict 查找（~20ns），每帧 2-20 次调用可忽略 |
| 公式系数放 GroundSystemConfigSO | A: CharacterConst 硬编码常量 → 不数据驱动。C: 新建 PropertyDef → 全局常数放 per-entity 树语义略歪 | GroundSystemConfigSO 已是「所有角色共享的物理参数」，注释自述「世界级定义」，加 movement formula 系数不违和 |
| motionSpeedScale 单变量（删除中间层 speedCoefficient） | — | 姿势系数删除后两个值相等，多余赋值无意义 |
| 姿势系数不参与速度计算 | A: 在公式中乘 0.5/0.3 姿势系数 → 双重惩罚（LocomotionAnimationSetSO.GetNativeSpeed 已编码 Crawl=1.0 vs Walk=1.5） | 动画系统已编码姿势差异，不应再叠一层 |

## Known Issues

- [ ] 护甲/背包 MoveSpeedPenalty 未聚合 — ArmorBase 和 Backpack 树有独立的 penalty 属性，S1 暂不跨树读取（Phase 5 装备系统落地时处理）
- [ ] Owner.AnimSet 与 ResolvedLocoAnimSet 可能不同实例 — BaseMovingState 用乘法 `nativeSpeed × scale` 消除差异，未来可改为直接读 `Discrete.EffectiveMaxSpeed`
- [ ] CharacterActor.Debug 中两个 GetFloat 调用不在 UNITY_EDITOR 条件编译内 — 无影响（Editor-only 类），但应统一

## Cross-References

### Related Sessions
- [2026-07-05-hit-reaction-pipeline.md](2026-07-05-hit-reaction-pipeline.md) — S3 受击管线同天完成，S3→S1 切换
- [2026-06-18-properties-接管角色物理.md](2026-06-18-properties-接管角色物理.md) — Physique 最初创建时间

### Related Plans
- [../plans/short-term.md](../plans/short-term.md) — S1 Properties 深度接入，已标记完成

### Related Tech Docs
- [../tech/L2-services/L2-modules/L3-character/animation/drivers/locomotion/locomotion-driver.md](../tech/L2-services/L2-modules/L3-character/animation/drivers/locomotion/locomotion-driver.md) — Stance / GroundLocomotion / BaseMovingState 三组件
- [../tech/L2-services/L2-modules/L3-properties/property-inventory.md](../tech/L2-services/L2-modules/L3-properties/property-inventory.md) — 速度相关属性定义（Agility、CarryWeight、MoveSpeed）

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactor + properties calibration. 公式系数（agilitySpeedBonus=0.03、weightPenaltyRatio=0.2）为 S1 占位值，正式数值在 Phase 5 装备系统落地后统一规划。
