# 短期开发计划 — 围绕 Character

> 日期: 2026-05-09
> 范围: P0 阶段，以 Character 为中心展开
> 原则: 每步有可玩增量，不预建空架子

## 路线总览

```
Phase 1 ──→ Phase 1.5 ──→ Phase 2 ──→ Phase 3 ──→ Phase 4
Loco完结   音效骨架     数值系统     生存+HUD    战斗基础
(已完成)    (已完成)     (进行中)    (后续)      (后续)
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

## Phase 2: 通用数值系统 🔄

**目标**: 项目级 Stats 基础设施，角色作为首批消费者。

### 已完成

| 功能 | 状态 |
|------|------|
| StatsTreeSO + StatsNodeSO (树形SO) | ✅ |
| InheritsFrom 继承 + Resolve() | ✅ |
| ResolvedStat 不修改原始 SO | ✅ |
| StatDefSO + StatInstance + StatFactory | ✅ |
| CharacterStats 容器 + Actor 接入 | ✅ |
| StatsTreeWindow EditorWindow | ✅ |
| 基本 StatDef (HP/Hunger/Thirst/Stamina + 6 Attributes) | ✅ |
| Debug 打印 | ✅ |

### 待完成

| 功能 | 说明 |
|------|------|
| BindAll 自动接线 | ConditionId → 条件表, DepleteTarget → 归零链 |
| 写入 SCharacterSnapshot | 外部 HUD/AI 只读 |
| HUD 面板 | 后续 Phase |

---

## Phase 3: 战斗基础

**目标**: 能做最简单的近战攻击。

| 子项 | 说明 |
|------|------|
| 近战攻击 | 鼠标左键触发，射线/碰撞检测 |
| 伤害判定 | 武器伤害 → 目标 HP 扣除 |
| 武器数据 | ScriptableObject，伤害值/攻速 |
| 基础反馈 | 命中音效/特效 |

**依赖**: Phase 2 的 HP 系统
**可玩增量**: 能砍丧尸，战斗循环建立

---

## Phase 4: 角色动画增强

**目标**: 受伤有视觉反馈，上半身动画独立。

| 子项 | 说明 |
|------|------|
| Hit React | 受击时播放受击动画 |
| UpperBody 覆盖 | 利用 Layer 1 + mask，上半身独立于下肢播放 |
| 多层仲裁雏形 | 为 UpperBody/Additive/Facial 铺路 |

**依赖**: Phase 3 + 多层仲裁器（同步实现）
**可玩增量**: 受击有反应，动画更自然

---

## 延后

- Vault / StepOver 障碍物穿越
- 姿势物理联动（crouch/prone）
- Crawl 爬行
