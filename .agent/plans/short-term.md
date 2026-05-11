# 短期开发计划 — 围绕 Character

> 日期: 2026-05-09
> 范围: P0 阶段，以 Character 为中心展开
> 原则: 每步有可玩增量，不预建空架子

## 路线总览

```
Phase 1 ──→ Phase 1.5 ──→ Phase 2 ──→ Phase 2.5 ──→ Phase 3 ──→ Phase 4 ──→ Phase 5
Loco完结   音效骨架     数值系统     Stats管理    HUD UI     战斗基础    动画增强
(已完成)    (已完成)     (已完成)    (进行中)     (后续)      (后续)      (后续)
```

---

## Phase 1: LocomotionSystem 完结 ✅

**目标**: 运动系统达到可封装里程碑。

| 任务 | 状态 |
|------|------|
| HeadLook (归一化/平滑/冻结) | ✅ |
| Footstep (Animancer事件注入) | ✅ |

---

## Phase 1.5: 音效系统骨架 ✅

**目标**: 搭建音效系统最小骨架。

| 子项 | 状态 |
|------|------|
| AudioSetSO / AudioRequest / AudioResponse | ✅ |
| AudioChannel (static) | ✅ |
| CharacterAudio + FootstepSetSO | ✅ |
| 脚步回路接通 | ✅ |

---

## Phase 2: 通用数值系统 ✅

**目标**: 项目级 Stats 基础设施，角色作为首批消费者。

### 已完成

| 功能 | 状态 |
|------|------|
| StatsTreeSO + StatsNodeSO (树形SO) | ✅ |
| InheritsFrom 继承 + Resolve() | ✅ |
| 树路径做 Key（"Vitals/HP"） | ✅ |
| 能力接口（IStatConsumable/IStatRestorable/IStatCumulative）| ✅ |
| 修改器系统（StatModifier + ModifierContext 并行槽位）| ✅ |
| StatInstance.Tick 接口分派 + 修改器管道 | ✅ |
| StatDefSO + StatFactory | ✅ |
| CharacterStats 容器（路径 key）+ Actor 接入 | ✅ |
| CharacterStats.All 对外只读字典 | ✅ |
| StatsTreeWindow EditorWindow | ✅ |
| 基本 StatDef (HP/Hunger/Thirst/Stamina + 6 Attributes) | ✅ |
| Debug 打印 | ✅ |
| 去掉 StatType enum + Behaviors/ 目录 | ✅ |

### 划掉的探索

- ~~BindAll 概念~~ — 实为构造末尾一次字符串→引用解析，已合并进 CharacterStats
- ~~ConditionId/Condition 条件表~~ — 业务逻辑收敛于 Character，外部干预走修改器
- ~~SCharacterStatsSnapshot 独立 struct~~ — 直接暴露 `IReadOnlyDictionary<string, StatInstance>`

---

## Phase 2.5: Character Stats 管理 🔄

**目标**: Character 作为决策者，消费数值系统。Stat 自行 Tick，Character 管修改器、归零链、伤害入口。

| 子项 | 说明 |
|------|------|
| 体力冲刺 | 冲刺时 addModifier 加速消耗，停止时 remove |
| 饥饿归零扣血 | CharacterActor.Update 中检查，hunger==0 → hp.Modify(-damage) |
| 伤害入口 | `TakeDamage(float)` → hp.Modify(-amount)，外部系统统一入口 |
| Stats 推 GameContext | 每帧更新，UI 可读（暂用 `stats.All` 直接暴露） |

**依赖**: Phase 2 完整
**可玩增量**: 跑起来体力降，饿了扣血，能受伤

---

## Phase 3: 基本 HUD UI

**目标**: 能看到数值变化，测试有反馈。

| 子项 | 说明 |
|------|------|
| Main UI | 主菜单占位，进入游戏按钮 |
| In-Game HUD | 左上显示 HP/Hunger/Thirst/Stamina 数值条 |
| 数据源 | 读 `GameContext` 中 CharacterStats 的快照 |

**依赖**: Phase 2.5 Stats 管理
**可玩增量**: 跑动/等待时能看到数值实时变化

---

## Phase 4: 战斗基础

**目标**: 能做最简单的近战攻击。

| 子项 | 说明 |
|------|------|
| 近战攻击 | 鼠标左键触发，射线/碰撞检测 |
| 伤害判定 | 武器伤害 → 目标 HP 扣除 |
| 武器数据 | ScriptableObject，伤害值/攻速 |
| 基础反馈 | 命中音效/特效 |

**依赖**: Phase 2.5 的 HP 系统 + 伤害入口
**可玩增量**: 能砍丧尸，战斗循环建立

---

## Phase 5: 角色动画增强

**目标**: 受伤有视觉反馈，上半身动画独立。

| 子项 | 说明 |
|------|------|
| Hit React | 受击时播放受击动画 |
| UpperBody 覆盖 | 利用 Layer 1 + mask，上半身独立于下肢播放 |
| 多层仲裁雏形 | 为 UpperBody/Additive/Facial 铺路 |

**依赖**: Phase 4 + 多层仲裁器（同步实现）
**可玩增量**: 受击有反应，动画更自然

---

## 延后

- Vault / StepOver 障碍物穿越
- 姿势物理联动（crouch/prone）
- Crawl 爬行
